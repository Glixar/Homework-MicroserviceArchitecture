namespace AuthService.Contracts.Responses;

/// <summary>
///     Результат создания пользователя.
/// </summary>
public sealed record CreateUserResponse(Guid Id);

/// <summary>
///     Результат логического удаления (soft-delete) пользователя.
/// </summary>
public sealed record DeleteUserResponse(Guid Id);

/// <summary>
///     Пагинированный ответ со списком пользователей.
/// </summary>
public sealed record GetAllUsersResponse(
    IReadOnlyList<UserAdminItemResponse> Items,
    int Total,
    int Offset,
    int Limit
);

/// <summary>
///     Результат восстановления пользователя.
/// </summary>
public sealed record RestoreUserResponse(Guid Id);

/// <summary>
///     Результат обновления пользователя.
/// </summary>
public sealed record UpdateUserResponse(Guid Id);

/// <summary>
///     Элемент выдачи пользователя для админ-панели.
/// </summary>
public sealed record UserAdminItemResponse(
    Guid Id,
    string? Email,
    string? FullName,
    string? Description,
    bool IsDeleted,
    DateTime? DeletedAtUtc,
    bool EmailConfirmed,
    bool LockoutEnabled,
    DateTimeOffset? LockoutEnd
);