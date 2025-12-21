using AuthService.Application.Abstractions;
using AuthService.Application.Commands.AdminPanel.Commands;
using AuthService.Contracts.Responses;
using AuthService.Domain;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace AuthService.Application.Commands.AdminPanel.Handlers;

public sealed class DeleteUserByIdHandler
    : ICommandHandler<DeleteUserResponse, DeleteUserByIdCommand>
{
    private readonly ILogger<DeleteUserByIdHandler> _logger;
    private readonly IRefreshSessionManager _refreshSessions;
    private readonly UserManager<User> _userManager;

    public DeleteUserByIdHandler(
        UserManager<User> userManager,
        IRefreshSessionManager refreshSessions,
        ILogger<DeleteUserByIdHandler> logger)
    {
        _userManager = userManager;
        _refreshSessions = refreshSessions;
        _logger = logger;
    }

    public async Task<Result<DeleteUserResponse, ErrorList>> Handle(
        DeleteUserByIdCommand command,
        CancellationToken ct)
    {
        User? user = await _userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == command.Id, ct);

        if (user is null)
        {
            _logger.LogWarning("Удаление отклонено: пользователь не найден {UserId}", command.Id);
            return Result.Failure<DeleteUserResponse, ErrorList>(
                Errors.General.NotFound(command.Id.ToString(), "Пользователь").ToErrorList());
        }

        if (user.IsDeleted)
        {
            _logger.LogInformation("Пользователь уже помечен как удалён (идемпотентно): {UserId}", user.Id);
            return Result.Success<DeleteUserResponse, ErrorList>(new DeleteUserResponse(user.Id));
        }

        // инвалидируем refresh-сессии
        UnitResult<Error> sessionsDelete = await _refreshSessions.DeleteAllByUserId(user.Id, ct);
        if (sessionsDelete.IsFailure)
        {
            _logger.LogError("Не удалось удалить refresh-сессии пользователя {UserId}: {Code} {Message}",
                user.Id, sessionsDelete.Error.Code, sessionsDelete.Error.Message);
            return Result.Failure<DeleteUserResponse, ErrorList>(Errors.General.Failure().ToErrorList());
        }

        // помечаем как удалён
        user.SoftDelete();

        IdentityResult update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            _logger.LogError("Soft-delete не выполнен для {UserId}: {Errors}",
                user.Id, string.Join(", ", update.Errors.Select(e => $"{e.Code}:{e.Description}")));
            return Result.Failure<DeleteUserResponse, ErrorList>(Errors.General.Failure().ToErrorList());
        }

        _logger.LogInformation("Пользователь {UserId} помечен как удалён (soft-delete).", user.Id);
        return Result.Success<DeleteUserResponse, ErrorList>(new DeleteUserResponse(user.Id));
    }
}