using AuthService.Application.Abstractions;
using AuthService.Application.Commands.Auth.Commands;
using AuthService.Contracts.Responses;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace AuthService.Application.Commands.Auth.Handlers;

public sealed class LogoutHandler : ICommandHandler<LogoutResponse, LogoutCommand>
{
    private readonly ICurrentUser _current;
    private readonly IRefreshSessionManager _refreshSessions;
    private readonly ILogger<LogoutHandler> _logger;

    public LogoutHandler(
        ICurrentUser current,
        IRefreshSessionManager refreshSessions,
        ILogger<LogoutHandler> logger)
    {
        _current = current;
        _refreshSessions = refreshSessions;
        _logger = logger;
    }

    public async Task<Result<LogoutResponse, ErrorList>> Handle(LogoutCommand command, CancellationToken ct)
    {
        var userIdOpt = _current.UserId;
        if (!_current.IsAuthenticated || !userIdOpt.HasValue)
        {
            _logger.LogWarning("Выход отклонён: пользователь не аутентифицирован.");
            return Result.Failure<LogoutResponse, ErrorList>(Errors.Tokens.InvalidToken().ToErrorList());
        }

        Guid userId = userIdOpt.Value;

        // Выход со всех устройств
        if (command.AllDevices)
        {
            _logger.LogInformation("Выход со всех устройств: пользователь {UserId}", userId);

            var delAll = await _refreshSessions.DeleteAllByUserId(userId, ct);
            if (delAll.IsFailure)
            {
                _logger.LogError("Ошибка удаления всех refresh-сессий пользователя {UserId}: {Error}",
                    userId, delAll.Error);
                return Result.Failure<LogoutResponse, ErrorList>(delAll.Error.ToErrorList());
            }

            _logger.LogInformation("Удалены все refresh-сессии пользователя {UserId}", userId);
            return Result.Success<LogoutResponse, ErrorList>(
                new LogoutResponse(true, "all", DateTimeOffset.UtcNow));
        }

        // Выход из одной конкретной сессии по refreshToken
        if (command.RefreshToken == Guid.Empty)
        {
            _logger.LogWarning("Не передан refreshToken для выхода из одной сессии.");
            return Result.Failure<LogoutResponse, ErrorList>(
                Errors.General.ValueIsInvalid("refreshToken").ToErrorList());
        }

        var sessionRes = await _refreshSessions.GetByRefreshToken(command.RefreshToken.ToString(), ct);
        if (sessionRes.IsFailure)
        {
            _logger.LogWarning("Refresh-сессия с токеном {RefreshToken} не найдена. Пользователь {UserId}",
                command.RefreshToken, userId);
            return Result.Failure<LogoutResponse, ErrorList>(sessionRes.Error.ToErrorList());
        }

        var session = sessionRes.Value;
        if (session.UserId != userId)
        {
            _logger.LogWarning(
                "Попытка удалить чужую refresh-сессию. CurrentUserId={CurrentUserId}, SessionUserId={SessionUserId}",
                userId, session.UserId);
            return Result.Failure<LogoutResponse, ErrorList>(Errors.General.Forbidden().ToErrorList());
        }

        var del = await _refreshSessions.Delete(session, ct);
        if (del.IsFailure)
        {
            _logger.LogError("Ошибка удаления refresh-сессии {SessionId} пользователя {UserId}: {Error}",
                session.Id, userId, del.Error);
            return Result.Failure<LogoutResponse, ErrorList>(del.Error.ToErrorList());
        }

        _logger.LogInformation("Удалена refresh-сессия {SessionId} пользователя {UserId}", session.Id, userId);

        return Result.Success<LogoutResponse, ErrorList>(
            new LogoutResponse(true, "current", DateTimeOffset.UtcNow));
    }
}