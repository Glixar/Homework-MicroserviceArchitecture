namespace AuthService.Contracts.Users;

/// <summary>
/// DTO пользователя, отдаваемое наружу.
/// </summary>
public sealed class UserResponse
{
    // Идентификатор пользователя
    public int Id { get; init; }

    // Логин / имя пользователя
    public string UserName { get; init; } = default!;

    // Имя
    public string FirstName { get; init; } = default!;

    // Фамилия
    public string LastName { get; init; } = default!;

    // Почта
    public string Email { get; init; } = default!;

    // Телефон
    public string Phone { get; init; } = default!;
}