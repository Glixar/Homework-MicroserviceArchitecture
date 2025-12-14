using AuthService.Domain;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Postgres;

/// <summary>
/// EF Core контекст для работы с PostgreSQL.
/// </summary>
public sealed class PostgresDbContext : DbContext
{
    /// <summary>
    /// Конструктор контекста.
    /// </summary>
    public PostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Набор пользователей.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUsers(modelBuilder);
    }

    /// <summary>
    /// Конфигурация сущности пользователя.
    /// </summary>
    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();

        user.ToTable("users");

        user.HasKey(x => x.Id);

        user.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        user.Property(x => x.UserName)
            .HasColumnName("user_name")
            .IsRequired();

        user.Property(x => x.FirstName)
            .HasColumnName("first_name")
            .IsRequired();

        user.Property(x => x.LastName)
            .HasColumnName("last_name")
            .IsRequired();

        user.Property(x => x.Email)
            .HasColumnName("email")
            .IsRequired();
    }
}