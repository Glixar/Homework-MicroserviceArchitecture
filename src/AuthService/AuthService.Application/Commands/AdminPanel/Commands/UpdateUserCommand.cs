using AuthService.Application.Abstractions;

namespace AuthService.Application.Commands.AdminPanel.Commands;

/// <summary>
///     Обновление пользователя (все поля опциональны; Lockout — включение/выключение блокировки).
/// </summary>
public sealed record UpdateUserCommand(
    Guid Id,
    string? Email,
    string? FullName,
    string? Description,
    bool? Lockout,
    IReadOnlyList<string>? Roles,
    IReadOnlyList<string>? Permissions
) : ICommand;