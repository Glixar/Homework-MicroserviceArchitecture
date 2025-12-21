using AuthService.Application.Abstractions;
using AuthService.Application.Commands.Accounts.Commands;
using AuthService.Contracts.Responses;
using AuthService.Domain;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace AuthService.Application.Commands.Accounts.Handlers;

public class UpdateMyProfileHandler : ICommandHandler<MyProfileResponse, UpdateMyProfileCommand>
{
    private readonly ILogger<UpdateMyProfileHandler> _logger;
    private readonly ICurrentUser _current;
    private readonly UserManager<User> _userManager;

    public UpdateMyProfileHandler(
        UserManager<User> userManager,
        ICurrentUser current,
        ILogger<UpdateMyProfileHandler> logger)
    {
        _userManager = userManager;
        _current = current;
        _logger = logger;
    }

    public async Task<Result<MyProfileResponse, ErrorList>> Handle(
        UpdateMyProfileCommand command,
        CancellationToken ct)
    {
        _logger.LogInformation("Начата обработка команды обновления профиля пользователя.");

        if (!_current.IsAuthenticated || !_current.UserId.HasValue)
        {
            _logger.LogWarning("Отсутствует access token.");
            return Result.Failure<MyProfileResponse, ErrorList>(
                Errors.Tokens.InvalidToken().ToErrorList());
        }

        string userIdStr = _current.UserId?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userIdStr))
        {
            _logger.LogWarning("Не удалось извлечь идентификатор пользователя из access token.");
            return Result.Failure<MyProfileResponse, ErrorList>(
                Errors.Tokens.InvalidToken().ToErrorList());
        }

        User? user = await _userManager.FindByIdAsync(userIdStr);
        if (user is null)
        {
            _logger.LogWarning("Пользователь не найден. UserId={UserId}", userIdStr);
            return Result.Failure<MyProfileResponse, ErrorList>(
                Errors.General.NotFound(userIdStr, "Пользователь").ToErrorList());
        }

        bool changed = false;

        if (!string.IsNullOrWhiteSpace(command.FullName) && command.FullName != user.UserName)
        {
            user.UserName = command.FullName;
            changed = true;
        }

        if (command.Description != user.Description)
        {
            user.Description = command.Description;
            changed = true;
        }

        if (!changed)
        {
            _logger.LogInformation("Нет изменений для сохранения. UserId={UserId}", user.Id);
            return Result.Success<MyProfileResponse, ErrorList>(
                new MyProfileResponse(user.UserName ?? string.Empty, user.Email ?? string.Empty, user.Description ?? string.Empty));
        }

        IdentityResult update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            _logger.LogWarning(
                "Ошибка при сохранении профиля пользователя. UserId={UserId}. Errors={Errors}",
                user.Id, string.Join(", ", update.Errors.Select(e => e.Description)));
            return Result.Failure<MyProfileResponse, ErrorList>(
                Errors.General.Failure("Не удалось обновить профиль").ToErrorList());
        }

        _logger.LogInformation("Профиль пользователя успешно обновлён. UserId={UserId}", user.Id);
        return Result.Success<MyProfileResponse, ErrorList>(
            new MyProfileResponse(user.UserName ?? string.Empty, user.Email ?? string.Empty, user.Description ?? string.Empty));
    }
}