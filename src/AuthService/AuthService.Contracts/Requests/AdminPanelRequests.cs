namespace AuthService.Contracts.Requests;

/// <summary>
///     Создание пользователя администратором.
/// </summary>
public sealed record CreateUserRequest(
    string Email,
    string Password,
    string FullName,
    string[] Roles,
    string[] Permissions,
    string? Description = null);

/// <summary>
///     Мягкое удаление пользователя по идентификатору.
/// </summary>
public sealed record DeleteUserByIdRequest(
    Guid Id);

/// <summary>
///     Получение списка пользователей с пагинацией.
/// </summary>
public sealed record GetAllUsersRequest(
    int Offset = 0,
    int Limit = 50,
    bool IncludeDeleted = false);

/// <summary>
///     Получение пользователя по e-mail.
/// </summary>
public sealed record GetUserByEmailRequest(
    string Email,
    bool IncludeDeleted = false);

/// <summary>
///     Получение пользователя по идентификатору.
/// </summary>
public sealed record GetUserByIdRequest(
    Guid Id,
    bool IncludeDeleted = false);

/// <summary>
///     Восстановление ранее удалённого пользователя.
/// </summary>
public sealed record RestoreUserRequest(
    Guid Id);

/// <summary>
///     Обновление свойств пользователя администратором.
///     Любое поле, равное null, не изменяется.
/// </summary>
public sealed record UpdateUserRequest(
    Guid Id,
    string? Email = null,
    string? FullName = null,
    string? Description = null,
    bool? Lockout = null,
    string[]? Roles = null,
    string[]? Permissions = null);