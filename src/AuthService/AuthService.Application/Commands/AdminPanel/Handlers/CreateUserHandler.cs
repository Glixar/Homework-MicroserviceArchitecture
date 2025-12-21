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

public sealed class CreateUserHandler
    : ICommandHandler<CreateUserResponse, CreateUserCommand>
{
    private readonly ILogger<CreateUserHandler> _logger;
    private readonly IPermissionManager _permissionManager;
    private readonly UserManager<User> _userManager;

    public CreateUserHandler(
        UserManager<User> userManager,
        IPermissionManager permissionManager,
        ILogger<CreateUserHandler> logger)
    {
        _userManager = userManager;
        _permissionManager = permissionManager;
        _logger = logger;
    }

    public async Task<Result<CreateUserResponse, ErrorList>> Handle(
        CreateUserCommand command,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return Result.Failure<CreateUserResponse, ErrorList>(
                Errors.General.ValueIsRequired(nameof(command.Email)).ToErrorList());
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return Result.Failure<CreateUserResponse, ErrorList>(
                Errors.General.ValueIsRequired(nameof(command.Password)).ToErrorList());
        }

        if (string.IsNullOrWhiteSpace(command.FullName))
        {
            return Result.Failure<CreateUserResponse, ErrorList>(
                Errors.General.ValueIsRequired(nameof(command.FullName)).ToErrorList());
        }

        if (command.Roles is null || command.Roles.Count == 0)
        {
            return Result.Failure<CreateUserResponse, ErrorList>(
                Errors.General.ValueIsRequired(nameof(command.Roles)).ToErrorList());
        }

        if (command.Permissions is null || command.Permissions.Count == 0)
        {
            return Result.Failure<CreateUserResponse, ErrorList>(
                Errors.General.ValueIsRequired(nameof(command.Permissions)).ToErrorList());
        }

        string normalized = command.Email.Trim().ToUpperInvariant();

        User? existing = await _userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, ct);

        if (existing is not null)
        {
            _logger.LogWarning("Создание отклонено: email уже занят (включая удалённых): {Email}", command.Email);
            return Result.Failure<CreateUserResponse, ErrorList>(
                Errors.General.AlreadyExist("Пользователь с таким email").ToErrorList());
        }

        User user = User.CreateUser(
            command.FullName,
            command.Email,
            command.Description ?? string.Empty);

        IdentityResult createRes = await _userManager.CreateAsync(user, command.Password);
        if (!createRes.Succeeded)
        {
            _logger.LogError("Создание пользователя не выполнено: {Errors}",
                string.Join(", ", createRes.Errors.Select(e => $"{e.Code}:{e.Description}")));
            return Result.Failure<CreateUserResponse, ErrorList>(Errors.General.Failure().ToErrorList());
        }


        IdentityResult roleRes = await _userManager.AddToRolesAsync(user, command.Roles);
        if (!roleRes.Succeeded)
        {
            _logger.LogError("Назначение ролей не выполнено для {UserId}: {Errors}",
                user.Id, string.Join(", ", roleRes.Errors.Select(e => $"{e.Code}:{e.Description}")));
            return Result.Failure<CreateUserResponse, ErrorList>(Errors.General.Failure().ToErrorList());
        }


        UnitResult<Error> permRes =
            await _permissionManager.ReplaceUserPermissionsAsync(user.Id, command.Permissions, ct);
        if (permRes.IsFailure)
        {
            _logger.LogError("Назначение пермишенов не выполнено для {UserId}: {Code} {Message}",
                user.Id, permRes.Error.Code, permRes.Error.Message);
            return Result.Failure<CreateUserResponse, ErrorList>(Errors.General.Failure().ToErrorList());
        }

        _logger.LogInformation("Пользователь создан: {UserId}", user.Id);
        return Result.Success<CreateUserResponse, ErrorList>(new CreateUserResponse(user.Id));
    }
}