using AuthService.Application.Abstractions;

namespace AuthService.Application.Commands.AdminPanel.Commands;

/// <summary>
///     Получить пользователя по идентификатору (с опцией включения удалённых).
/// </summary>
public sealed record GetUserByIdCommand(
    Guid Id,
    bool IncludeDeleted
) : ICommand;