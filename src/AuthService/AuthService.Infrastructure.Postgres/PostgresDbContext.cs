using System.Reflection;
using AuthService.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Postgres;

/// <summary>
///     Главный DbContext (PostgreSQL) с глобальным фильтром на мягко-удалённых пользователей.
/// </summary>
public sealed class PostgresDbContext
    : IdentityDbContext<
        User,
        Role,
        Guid,
        IdentityUserClaim<Guid>,
        IdentityUserRole<Guid>,
        IdentityUserLogin<Guid>,
        IdentityRoleClaim<Guid>,
        IdentityUserToken<Guid>>
{
    public PostgresDbContext(DbContextOptions<PostgresDbContext> options)
        : base(options)
    {
    }

    public DbSet<Permission> Permissions { get; set; } = null!;

    public DbSet<RolePermission> RolePermissions { get; set; } = null!;

    public DbSet<AdminAccount> AdminAccounts { get; set; } = null!;

    public DbSet<RefreshSession> RefreshSessions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Схема по умолчанию
        builder.HasDefaultSchema("accounts");

        // Подтягиваем все IEntityTypeConfiguration<> из сборки
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Глобальный фильтр на soft-delete (читатели не видят удалённых)
        builder.Entity<User>()
            .HasQueryFilter(u => !u.IsDeleted);

        // Значение по умолчанию для soft delete
        builder.Entity<User>()
            .Property(u => u.IsDeleted)
            .HasDefaultValue(false);

        // Делаем UserName (NormalizedUserName) НЕ уникальным.
        builder.Entity<User>()
            .HasIndex(u => u.NormalizedUserName)
            .IsUnique(false);

        // Email остаётся уникальным.
        builder.Entity<User>()
            .HasIndex(u => u.NormalizedEmail)
            .IsUnique();
    }
}