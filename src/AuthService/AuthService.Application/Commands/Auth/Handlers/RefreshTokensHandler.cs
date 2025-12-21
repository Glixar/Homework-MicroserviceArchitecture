using AuthService.Application.Abstractions;
using AuthService.Application.Commands.Auth.Commands;
using AuthService.Application.JWT;
using AuthService.Contracts.Responses;
using AuthService.Domain;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace AuthService.Application.Commands.Auth.Handlers;

public sealed class RefreshTokensHandler : ICommandHandler<TokensResponse, RefreshTokensCommand>
{
    private readonly ICurrentUser _current;
    private readonly ILogger<RefreshTokensHandler> _logger;
    private readonly IRefreshSessionManager _refreshSessions;
    private readonly ITokenProvider _tokenProvider;
    private readonly UserManager<User> _userManager;

    public RefreshTokensHandler(
        ICurrentUser current,
        ILogger<RefreshTokensHandler> logger,
        IRefreshSessionManager refreshSessions,
        ITokenProvider tokenProvider,
        UserManager<User> userManager)
    {
        _current = current;
        _logger = logger;
        _refreshSessions = refreshSessions;
        _tokenProvider = tokenProvider;
        _userManager = userManager;
    }

    public async Task<Result<TokensResponse, ErrorList>> Handle(RefreshTokensCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Обновление токенов: старт");

        if (command.RefreshToken == Guid.Empty)
        {
            _logger.LogWarning("Обновление токенов: не передан refreshToken");
            return Result.Failure<TokensResponse, ErrorList>(
                Errors.General.ValueIsRequired(nameof(command.RefreshToken)).ToErrorList());
        }

        // Ищем refresh-сессию
        var sessionRes = await _refreshSessions.GetByRefreshToken(command.RefreshToken.ToString(), ct);
        if (sessionRes.IsFailure)
        {
            _logger.LogWarning("Обновление токенов: refresh-сессия не найдена.");
            return Result.Failure<TokensResponse, ErrorList>(sessionRes.Error.ToErrorList());
        }

        RefreshSession session = sessionRes.Value;

        // Проверяем владельца
        if (_current.IsAuthenticated && _current.UserId is Guid currentUserId && session.UserId != currentUserId)
        {
            _logger.LogWarning(
                "Обновление токенов: несоответствие владельца refresh-сессии. Ожидался {Expected}, получен {Actual}.",
                currentUserId, session.UserId);
            return Result.Failure<TokensResponse, ErrorList>(
                Errors.General.Forbidden("Несоответствие владельца сессии").ToErrorList());
        }

        // Сверка пары по jti
        if (_current.SessionId is Guid currentJti && currentJti != session.Jti)
        {
            _logger.LogWarning(
                "Обновление токенов: несоответствие пары токенов (jti). Ожидался {ExpectedJti}, получен {ActualJti}.",
                session.Jti, currentJti);
            return Result.Failure<TokensResponse, ErrorList>(
                Errors.General.Forbidden("Несоответствие пары токенов").ToErrorList());
        }

        // Пользователь
        _logger.LogInformation("Обновление токенов: загрузка пользователя {UserId}", session.UserId);
        User? user = await _userManager.FindByIdAsync(session.UserId.ToString());
        if (user is null)
        {
            _logger.LogWarning("Обновление токенов: пользователь {UserId} не найден", session.UserId);
            return Result.Failure<TokensResponse, ErrorList>(
                Errors.General.NotFound(session.UserId.ToString(), "Пользователь").ToErrorList());
        }

        // Генерируем новую пару
        _logger.LogInformation("Обновление токенов: генерация новой пары токенов для пользователя {UserId}", user.Id);
        AccessTokenResult accessToken = await _tokenProvider.GenerateAccessToken(user, ct);
        RefreshSession newSession = await _tokenProvider.GenerateRefreshToken(user, accessToken.Jti, ct);

        // Удаляем старую сессию
        _logger.LogInformation("Обновление токенов: удаление старой сессии {SessionId}", session.Id);
        var del = await _refreshSessions.Delete(session, ct);
        if (del.IsFailure)
        {
            _logger.LogError("Обновление токенов: ошибка удаления старой сессии {SessionId}: {Error}",
                session.Id, del.Error);
            return Result.Failure<TokensResponse, ErrorList>(del.Error.ToErrorList());
        }

        _logger.LogInformation(
            "Обновление токенов: выдана новая пара токенов; истечение access {AccessExp:o}; истечение refresh {RefreshExp:o}; новая сессия {SessionId}",
            accessToken.ValidTo, newSession.ExpiresIn, newSession.Id);

        var payload = new TokensResponse(
            accessToken.AccessToken,
            accessToken.ValidTo,
            newSession.RefreshToken,
            newSession.ExpiresIn);

        return Result.Success<TokensResponse, ErrorList>(payload);
    }
}