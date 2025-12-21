using AuthService.Application.Abstractions;

namespace AuthService.Application.Commands.AdminPanel.Commands;

/// <summary>
///     Получить страницу пользователей (с опцией включения удалённых).
/// </summary>
public sealed record GetAllUsersCommand(
    int Offset,
    int Limit,
    bool IncludeDeleted
) : ICommand;