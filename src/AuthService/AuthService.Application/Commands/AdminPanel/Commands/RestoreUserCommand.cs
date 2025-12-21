using AuthService.Application.Abstractions;

namespace AuthService.Application.Commands.AdminPanel.Commands;

/// <summary>
///     Восстановление ранее логически удалённого пользователя.
/// </summary>
public sealed record RestoreUserCommand(
    Guid Id
) : ICommand;