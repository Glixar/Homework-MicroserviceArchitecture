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

/// <summary>
///     Восстанавливает ранее логически удалённого пользователя.
/// </summary>
public sealed class RestoreUserHandler
    : ICommandHandler<RestoreUserResponse, RestoreUserCommand>
{
    private readonly ILogger<RestoreUserHandler> _logger;
    private readonly UserManager<User> _userManager;

    public RestoreUserHandler(
        UserManager<User> userManager,
        ILogger<RestoreUserHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result<RestoreUserResponse, ErrorList>> Handle(
        RestoreUserCommand command,
        CancellationToken ct)
    {
        User? user = await _userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == command.Id, ct);

        if (user is null)
        {
            _logger.LogWarning("Восстановление отклонено: пользователь не найден {UserId}", command.Id);
            return Result.Failure<RestoreUserResponse, ErrorList>(
                Errors.General.NotFound(command.Id.ToString(), "Пользователь").ToErrorList());
        }

        if (!user.IsDeleted)
        {
            _logger.LogInformation("Пользователь уже активен (идемпотентно): {UserId}", user.Id);
            return Result.Success<RestoreUserResponse, ErrorList>(new RestoreUserResponse(user.Id));
        }

        // доменная логика восстановления
        user.Restore();

        IdentityResult update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            _logger.LogError("Восстановление не выполнено для {UserId}: {Errors}",
                user.Id, string.Join(", ", update.Errors.Select(e => $"{e.Code}:{e.Description}")));
            return Result.Failure<RestoreUserResponse, ErrorList>(Errors.General.Failure().ToErrorList());
        }

        _logger.LogInformation("Пользователь восстановлен: {UserId}", user.Id);
        return Result.Success<RestoreUserResponse, ErrorList>(new RestoreUserResponse(user.Id));
    }
}