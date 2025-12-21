using AuthService.Application.Abstractions;

namespace AuthService.Application.Commands.AdminPanel.Commands;

/// <summary>
///     Получить пользователя по e-mail (с опцией включения удалённых).
/// </summary>
public sealed record GetUserByEmailCommand(
    string Email,
    bool IncludeDeleted
) : ICommand;