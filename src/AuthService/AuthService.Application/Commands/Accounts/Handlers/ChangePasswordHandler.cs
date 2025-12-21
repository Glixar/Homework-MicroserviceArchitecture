using AuthService.Application.Abstractions;
using AuthService.Application.Commands.Accounts.Commands;
using AuthService.Contracts.Models;
using AuthService.Contracts.Responses;
using AuthService.Domain;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace AuthService.Application.Commands.Accounts.Handlers;

public class ChangePasswordHandler : ICommandHandler<ChangePasswordResponse, ChangePasswordCommand>
{
    private readonly ILogger<ChangePasswordHandler> _logger;
    private readonly IRefreshSessionManager _refreshSessions;
    private readonly ICurrentUser _current;
    private readonly UserManager<User> _userManager;

    public ChangePasswordHandler(
        UserManager<User> userManager,
        IRefreshSessionManager refreshSessions,
        ICurrentUser current,
        ILogger<ChangePasswordHandler> logger)
    {
        _userManager = userManager;
        _refreshSessions = refreshSessions;
        _current = current;
        _logger = logger;
    }

    public async Task<Result<ChangePasswordResponse, ErrorList>> Handle(
        ChangePasswordCommand command,
        CancellationToken ct)
    {
        _logger.LogInformation("Запрос на смену пароля получен");

        if (!_current.IsAuthenticated || !_current.UserId.HasValue)
        {
            return Result.Failure<ChangePasswordResponse, ErrorList>(
                Errors.Tokens.InvalidToken().ToErrorList());
        }

        if (string.IsNullOrWhiteSpace(command.CurrentPassword))
        {
            return Result.Failure<ChangePasswordResponse, ErrorList>(
                Errors.General.ValueIsRequired("AccessToken").ToErrorList()); // оставил исходную сигнатуру ошибки из файла
        }

        if (string.IsNullOrWhiteSpace(command.NewPassword))
        {
            return Result.Failure<ChangePasswordResponse, ErrorList>(
                Errors.General.ValueIsRequired("NewPassword").ToErrorList());
        }

        if (command.CurrentPassword == command.NewPassword)
        {
            return Result.Failure<ChangePasswordResponse, ErrorList>(
                Errors.General.ValueIsInvalid("NewPassword").ToErrorList());
        }

        string userIdStr = _current.UserId?.ToString() ?? string.Empty;
        if (!Guid.TryParse(userIdStr, out Guid userId))
        {
            return Result.Failure<ChangePasswordResponse, ErrorList>(
                Errors.General.ValueIsInvalid("userId").ToErrorList());
        }

        User? user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure<ChangePasswordResponse, ErrorList>(
                Errors.General.NotFound(userId.ToString(), "Пользователь").ToErrorList());
        }

        bool oldPasswordOk = await _userManager.CheckPasswordAsync(user, command.CurrentPassword);
        if (!oldPasswordOk)
        {
            return Result.Failure<ChangePasswordResponse, ErrorList>(
                Errors.General.ValueIsInvalid("CurrentPassword").ToErrorList());
        }

        IdentityResult update = await _userManager.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword);
        if (!update.Succeeded)
        {
            _logger.LogError("Не удалось изменить пароль пользователю {UserId}: {Errors}",
                user.Id, string.Join(", ", update.Errors.Select(e => e.Description)));
            return Result.Failure<ChangePasswordResponse, ErrorList>(
                Errors.General.Failure("Не удалось изменить пароль").ToErrorList());
        }

        await _userManager.UpdateSecurityStampAsync(user);

        UnitResult<Error> deleteAll = await _refreshSessions.DeleteAllByUserId(user.Id, ct);
        if (deleteAll.IsFailure)
        {
            _logger.LogError(
                "Пароль изменён, но не удалось удалить refresh-сессии пользователя {UserId}: {Error}",
                user.Id, deleteAll.Error.Message);

            return Result.Success<ChangePasswordResponse, ErrorList>(
                new ChangePasswordResponse("Пароль изменён, но активные сессии не удалось завершить", true));
        }

        _logger.LogInformation(
            "Пароль успешно изменён. Пользователь {UserId}", user.Id);

        return Result.Success<ChangePasswordResponse, ErrorList>(
            new ChangePasswordResponse("Пароль успешно изменён", true));
    }
}