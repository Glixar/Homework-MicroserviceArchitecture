using AuthService.Application.Abstractions;
using AuthService.Application.Commands.Accounts.Commands;
using AuthService.Contracts.Responses;
using AuthService.Domain;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace AuthService.Application.Commands.Accounts.Handlers;

public class DeleteMyProfileHandler : ICommandHandler<DeleteProfileResponse, DeleteMyProfileCommand>
{
    private readonly ILogger<DeleteMyProfileHandler> _logger;
    private readonly ICurrentUser _current;
    private readonly UserManager<User> _userManager;

    public DeleteMyProfileHandler(
        ILogger<DeleteMyProfileHandler> logger,
        ICurrentUser current,
        UserManager<User> userManager)
    {
        _logger = logger;
        _current = current;
        _userManager = userManager;
    }

    public async Task<Result<DeleteProfileResponse, ErrorList>> Handle(
        DeleteMyProfileCommand command,
        CancellationToken ct)
    {
        if (_current.UserId is null)
        {
            _logger.LogWarning("Удаление профиля: текущий пользователь не найден в контексте");

            return Result.Failure<DeleteProfileResponse, ErrorList>(
                Errors.General.Failure("Пользователь не авторизован.").ToErrorList());
        }

        Guid currentUserId = _current.UserId.Value;

        User? user = await _userManager.FindByIdAsync(currentUserId.ToString());
        if (user is null)
        {
            _logger.LogWarning(
                "Удаление профиля: пользователь {UserId} не найден",
                currentUserId);

            return Result.Failure<DeleteProfileResponse, ErrorList>(
                Errors.General.Failure("Пользователь не найден.").ToErrorList());
        }

        if (user.IsDeleted)
        {
            _logger.LogInformation(
                "Удаление профиля: пользователь {UserId} уже помечен как удалённый",
                currentUserId);

            return Result.Success<DeleteProfileResponse, ErrorList>(
                new DeleteProfileResponse("Профиль пользователя уже удалён"));
        }

        user.SoftDelete(currentUserId);

        user.SecurityStamp = Guid.NewGuid().ToString();

        IdentityResult updateUser = await _userManager.UpdateAsync(user);
        if (!updateUser.Succeeded)
        {
            _logger.LogError(
                "Удаление профиля: не удалось обновить пользователя {UserId}: {Error}",
                user.Id,
                string.Join(", ", updateUser.Errors.Select(e => e.Description)));

            return Result.Failure<DeleteProfileResponse, ErrorList>(
                Errors.General.Failure("Не удалось удалить аккаунт.").ToErrorList());
        }

        _logger.LogInformation(
            "Профиль пользователя {UserId} помечен как удалённый (soft delete).",
            user.Id);

        return Result.Success<DeleteProfileResponse, ErrorList>(
            new DeleteProfileResponse("Профиль пользователя удалён"));
    }
}