namespace AuthService.Domain;

/// <summary>
/// Админ-аккаунт
/// </summary>
public class AdminAccount
{
    public const string ADMIN = nameof(ADMIN);

    private AdminAccount()
    {
    }

    public AdminAccount(string fullName, User user)
    {
        Id = Guid.NewGuid();
        FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        User = user ?? throw new ArgumentNullException(nameof(user));
        UserId = user.Id;
    }

    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = default!;

    /// <summary>
    /// Полное имя администратора
    /// </summary>
    public string FullName { get; set; } = default!;
}