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
///     Обновляет поля пользователя, роли и пермишены.
/// </summary>
public sealed class UpdateUserHandler
    : ICommandHandler<UpdateUserResponse, UpdateUserCommand>
{
    private readonly ILogger<UpdateUserHandler> _logger;
    private readonly IPermissionManager _permissionManager;
    private readonly UserManager<User> _userManager;

    public UpdateUserHandler(
        UserManager<User> userManager,
        IPermissionManager permissionManager,
        ILogger<UpdateUserHandler> logger)
    {
        _userManager = userManager;
        _permissionManager = permissionManager;
        _logger = logger;
    }

    public async Task<Result<UpdateUserResponse, ErrorList>> Handle(
        UpdateUserCommand command,
        CancellationToken ct)
    {
        User? user = await _userManager.Users
            .IgnoreQueryFilters() // админ может редактировать и удалённого (например, для подготовки к restore)
            .FirstOrDefaultAsync(u => u.Id == command.Id, ct);

        if (user is null)
        {
            _logger.LogWarning("Обновление отклонено: пользователь не найден {UserId}", command.Id);
            return Result.Failure<UpdateUserResponse, ErrorList>(
                Errors.General.NotFound(command.Id.ToString(), "Пользователь").ToErrorList());
        }

        // Email (если передали) — проверяем уникальность по всей БД, включая soft-deleted
        if (!string.IsNullOrWhiteSpace(command.Email))
        {
            string normalized = command.Email.Trim().ToUpperInvariant();
            User? emailOwner = await _userManager.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized && u.Id != user.Id, ct);

            if (emailOwner is not null)
            {
                _logger.LogWarning("Изменение email отклонено: адрес занят (включая удалённых): {Email}",
                    command.Email);
                return Result.Failure<UpdateUserResponse, ErrorList>(
                    Errors.General.AlreadyExist("Пользователь с таким email").ToErrorList());
            }

            user.Email = command.Email;
            user.NormalizedEmail = normalized;
            // инвалидация токенов при смене критичных данных
            user.SecurityStamp = Guid.NewGuid().ToString("N");
        }

        // FullName -> используем как отображаемое имя (UserName)
        if (!string.IsNullOrWhiteSpace(command.FullName))
        {
            user.UserName = command.FullName;
            user.NormalizedUserName = command.FullName.ToUpperInvariant();
        }

        if (command.Description is not null)
        {
            user.Description = command.Description;
        }

        if (command.Lockout.HasValue)
        {
            if (command.Lockout.Value)
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;
            }
            else
            {
                user.LockoutEnabled = false;
                user.LockoutEnd = null;
            }

            user.SecurityStamp = Guid.NewGuid().ToString("N");
        }

        IdentityResult updateRes = await _userManager.UpdateAsync(user);
        if (!updateRes.Succeeded)
        {
            _logger.LogError("Обновление пользователя не выполнено {UserId}: {Errors}",
                user.Id, string.Join(", ", updateRes.Errors.Select(e => $"{e.Code}:{e.Description}")));
            return Result.Failure<UpdateUserResponse, ErrorList>(Errors.General.Failure().ToErrorList());
        }

        // Роли (если передали — полная замена набора)
        if (command.Roles is not null)
        {
            IList<string> currentRoles = await _userManager.GetRolesAsync(user);
            HashSet<string> desired = new(command.Roles);
            HashSet<string> current = new(currentRoles);

            string[] toRemove = current.Except(desired).ToArray();
            string[] toAdd = desired.Except(current).ToArray();

            if (toRemove.Length > 0)
            {
                IdentityResult rr = await _userManager.RemoveFromRolesAsync(user, toRemove);
                if (!rr.Succeeded)
                {
                    _logger.LogError("Снятие ролей не выполнено {UserId}: {Errors}",
                        user.Id, string.Join(", ", rr.Errors.Select(e => $"{e.Code}:{e.Description}")));
                    return Result.Failure<UpdateUserResponse, ErrorList>(Errors.General.Failure().ToErrorList());
                }
            }

            if (toAdd.Length > 0)
            {
                IdentityResult ar = await _userManager.AddToRolesAsync(user, toAdd);
                if (!ar.Succeeded)
                {
                    _logger.LogError("Назначение ролей не выполнено {UserId}: {Errors}",
                        user.Id, string.Join(", ", ar.Errors.Select(e => $"{e.Code}:{e.Description}")));
                    return Result.Failure<UpdateUserResponse, ErrorList>(Errors.General.Failure().ToErrorList());
                }
            }
        }

        if (command.Permissions is not null)
        {
            UnitResult<Error> permRes =
                await _permissionManager.ReplaceUserPermissionsAsync(user.Id, command.Permissions, ct);
            if (permRes.IsFailure)
            {
                _logger.LogError("Обновление пермишенов не выполнено {UserId}: {Code} {Message}",
                    user.Id, permRes.Error.Code, permRes.Error.Message);
                return Result.Failure<UpdateUserResponse, ErrorList>(Errors.General.Failure().ToErrorList());
            }
        }

        _logger.LogInformation("Пользователь обновлён: {UserId}", user.Id);
        return Result.Success<UpdateUserResponse, ErrorList>(new UpdateUserResponse(user.Id));
    }
}