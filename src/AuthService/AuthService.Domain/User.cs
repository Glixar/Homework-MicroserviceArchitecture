using Microsoft.AspNetCore.Identity;

namespace AuthService.Domain;

/// <summary>Сущность пользователя.</summary>
public class User : IdentityUser<Guid>
{
    private readonly List<Role> _roles = [];

    private User() { }

    /// <summary>Навигация на роли (read-only).</summary>
    public IReadOnlyList<Role> Roles => _roles;

    /// <summary>Описание.</summary>
    public string Description { get; set; }

    /// <summary>Флаг soft-delete.</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>UTC-время soft-delete.</summary>
    public DateTime? DeletedAtUtc { get; private set; }

    /// <summary>Инициатор soft-delete.</summary>
    public Guid? DeletedByUserId { get; private set; }

    /// <summary>
    ///     Soft-delete:
    ///     - включает блокировку входа (LockoutEnabled/LockoutEnd);
    ///     - обновляет SecurityStamp/ConcurrencyStamp;
    ///     - проставляет IsDeleted/DeletedAtUtc/DeletedByUserId.
    ///     Идемпотентно.
    /// </summary>
    public void SoftDelete(Guid? byUserId = null)
    {
        if (IsDeleted)
        {
            return;
        }

        LockoutEnabled = true;
        LockoutEnd = DateTimeOffset.MaxValue;

        SecurityStamp = Guid.NewGuid().ToString("N");
        ConcurrencyStamp = Guid.NewGuid().ToString("N");

        IsDeleted = true;
        DeletedAtUtc = DateTime.UtcNow;
        DeletedByUserId = byUserId;
    }

    /// <summary>
    ///     Отмена soft-delete:
    ///     - снимает блокировку входа;
    ///     - обновляет SecurityStamp/ConcurrencyStamp;
    ///     - сбрасывает поля удаления.
    ///     Идемпотентно.
    /// </summary>
    public void Restore()
    {
        if (!IsDeleted)
        {
            return;
        }

        LockoutEnabled = false;
        LockoutEnd = null;

        SecurityStamp = Guid.NewGuid().ToString("N");
        ConcurrencyStamp = Guid.NewGuid().ToString("N");

        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedByUserId = null;
    }

    /// <summary>Фабрика обычного пользователя.</summary>
    public static User CreateUser(string userName, string email, string description) =>
        new()
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = false,
            Description = description,
            IsDeleted = false,
            LockoutEnabled = false,
            LockoutEnd = null
        };
}