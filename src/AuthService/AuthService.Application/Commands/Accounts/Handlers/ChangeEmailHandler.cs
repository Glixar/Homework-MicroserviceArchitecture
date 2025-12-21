using AuthService.Application.Abstractions;
using AuthService.Application.Commands.Accounts.Commands;
using AuthService.Contracts.Responses;
using AuthService.Domain;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace AuthService.Application.Commands.Accounts.Handlers;

public class ChangeEmailHandler : ICommandHandler<ChangeEmailResponse, ChangeEmailCommand>
{
    private readonly ICurrentUser _current;
    private readonly ILogger<ChangeEmailHandler> _logger;
    private readonly IRefreshSessionManager _refreshSessions;
    private readonly UserManager<User> _userManager;

    public ChangeEmailHandler(
        UserManager<User> userManager,
        ICurrentUser current,
        IRefreshSessionManager refreshSessions,
        ILogger<ChangeEmailHandler> logger
    )
    {
        _current = current;
        _userManager = userManager;
        _refreshSessions = refreshSessions;
        _logger = logger;
    }

    public async Task<Result<ChangeEmailResponse, ErrorList>> Handle(
        ChangeEmailCommand command,
        CancellationToken ct)
    {
        _logger.LogInformation("Запрос на смену e-mail получен");

        if (!_current.IsAuthenticated || !_current.UserId.HasValue)
        {
            _logger.LogWarning("Не удалось извлечь Id пользователя из токена");
            return Result.Failure<ChangeEmailResponse, ErrorList>(
                Errors.Tokens.InvalidToken().ToErrorList());
        }

        if (string.IsNullOrWhiteSpace(command.NewEmail))
        {
            _logger.LogWarning("Новый e-mail не передан");
            return Result.Failure<ChangeEmailResponse, ErrorList>(
                Errors.General.ValueIsRequired("NewEmail").ToErrorList());
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            _logger.LogWarning("Пароль для подтверждения смены e-mail не передан");
            return Result.Failure<ChangeEmailResponse, ErrorList>(
                Errors.General.ValueIsRequired("Password").ToErrorList());
        }

        string userIdStr = _current.UserId?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userIdStr))
        {
            _logger.LogWarning("Не удалось извлечь Id пользователя из токена");
            return Result.Failure<ChangeEmailResponse, ErrorList>(
                Errors.Tokens.InvalidToken().ToErrorList());
        }

        User? user = await _userManager.FindByIdAsync(userIdStr);
        if (user is null)
        {
            _logger.LogWarning("Пользователь с Id {UserId} не найден", userIdStr);
            return Result.Failure<ChangeEmailResponse, ErrorList>(
                Errors.General.NotFound(userIdStr, "Пользователь").ToErrorList());
        }

        bool passwordOk = await _userManager.CheckPasswordAsync(user, command.Password);
        if (!passwordOk)
        {
            _logger.LogWarning("Неверный пароль при попытке смены e-mail для пользователя {UserId}", user.Id);
            return Result.Failure<ChangeEmailResponse, ErrorList>(
                Errors.General.ValueIsInvalid("Password").ToErrorList());
        }

        if (string.Equals(user.Email, command.NewEmail, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Новый e-mail совпадает с текущим у пользователя {UserId}", user.Id);
            return Result.Failure<ChangeEmailResponse, ErrorList>(
                Errors.General.ValueIsInvalid("NewEmail").ToErrorList());
        }

        User? exists = await _userManager.FindByEmailAsync(command.NewEmail);
        if (exists is not null)
        {
            _logger.LogWarning("Пользователь с e-mail {Email} уже существует", command.NewEmail);
            return Result.Failure<ChangeEmailResponse, ErrorList>(
                Errors.General.AlreadyExist("Пользователь").ToErrorList());
        }

        user.Email = command.NewEmail;
        user.UserName = command.NewEmail;

        IdentityResult updateEmail = await _userManager.UpdateAsync(user);
        if (!updateEmail.Succeeded)
        {
            _logger.LogError("Не удалось обновить e-mail пользователю {UserId}: {Errors}",
                user.Id, string.Join(", ", updateEmail.Errors.Select(e => e.Description)));
            return Result.Failure<ChangeEmailResponse, ErrorList>(
                Errors.General.Failure("Не удалось обновить e-mail").ToErrorList());
        }

        await _userManager.UpdateSecurityStampAsync(user);

        UnitResult<Error> deleteSessions = await _refreshSessions.DeleteAllByUserId(user.Id, ct);
        if (deleteSessions.IsFailure)
        {
            _logger.LogWarning("E-mail изменён, но не удалось инвалидировать refresh-сессии пользователя {UserId}",
                user.Id);
        }

        _logger.LogInformation("E-mail пользователя {UserId} успешно изменён на {NewEmail}", user.Id, command.NewEmail);

        return Result.Success<ChangeEmailResponse, ErrorList>(
            new ChangeEmailResponse("E-mail успешно изменён.", true));
    }
}