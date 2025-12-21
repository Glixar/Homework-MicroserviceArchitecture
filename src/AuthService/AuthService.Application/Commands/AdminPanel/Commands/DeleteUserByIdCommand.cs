using AuthService.Application.Abstractions;

namespace AuthService.Application.Commands.AdminPanel.Commands;

/// <summary>
///     Логическое удаление пользователя (soft-delete).
/// </summary>
public sealed record DeleteUserByIdCommand(
    Guid Id
) : ICommand;