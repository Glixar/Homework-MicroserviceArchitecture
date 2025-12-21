using AuthService.Application.Abstractions;

namespace AuthService.Application.Commands.AdminPanel.Commands;

/// <summary>
///     Создание пользователя (FullName, Roles и Permissions обязательны).
/// </summary>
public sealed record CreateUserCommand(
    string Email,
    string Password,
    string FullName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    string? Description
) : ICommand;