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

public sealed class LoginHandler
    : ICommandHandler<TokensResponse, LoginCommand>
{
    private readonly ILogger<LoginHandler> _logger;
    private readonly ITokenProvider _tokenProvider;
    private readonly UserManager<User> _userManager;

    public LoginHandler(
        UserManager<User> userManager,
        ITokenProvider tokenProvider,
        ILogger<LoginHandler> logger)
    {
        _userManager = userManager;
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    public async Task<Result<TokensResponse, ErrorList>> Handle(LoginCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Начало входа в систему.");

        try
        {
            if (string.IsNullOrWhiteSpace(command.Email) || string.IsNullOrWhiteSpace(command.Password))
            {
                _logger.LogWarning("Валидация не пройдена: пустой e-mail или пароль.");
                return Result.Failure<TokensResponse, ErrorList>(
                    Errors.General.ValueIsInvalid("email/password").ToErrorList());
            }

            User? user = await _userManager.FindByEmailAsync(command.Email);
            if (user is null)
            {
                _logger.LogWarning("Вход отклонён: пользователь с e-mail {Email} не найден.", command.Email);
                return Result.Failure<TokensResponse, ErrorList>(
                    Errors.User.InvalidCredentials().ToErrorList());
            }

            bool passwordOk = await _userManager.CheckPasswordAsync(user, command.Password);
            if (!passwordOk)
            {
                _logger.LogWarning("Вход отклонён: неверный пароль для пользователя {UserId}.", user.Id);
                return Result.Failure<TokensResponse, ErrorList>(
                    Errors.User.InvalidCredentials().ToErrorList());
            }

            _logger.LogDebug("Учётные данные подтверждены для пользователя {UserId}.", user.Id);

            AccessTokenResult accessToken = await _tokenProvider.GenerateAccessToken(user, ct);
            _logger.LogDebug("Сгенерирован токен доступа для пользователя {UserId} с JTI {Jti}.", user.Id, accessToken.Jti);

            RefreshSession refreshToken = await _tokenProvider.GenerateRefreshToken(user, accessToken.Jti, ct);
            _logger.LogDebug("Сгенерирована refresh-сессия для пользователя {UserId}.", user.Id);

            _logger.LogInformation("Вход выполнен успешно для пользователя {UserId}.", user.Id);

            return new TokensResponse(
                accessToken.AccessToken,
                accessToken.ValidTo,
                refreshToken.RefreshToken,
                refreshToken.ExpiresIn);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Вход отменён по запросу.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Неожиданная ошибка во время входа.");
            return Result.Failure<TokensResponse, ErrorList>(
                Errors.General.Unexpected().ToErrorList());
        }
    }
}