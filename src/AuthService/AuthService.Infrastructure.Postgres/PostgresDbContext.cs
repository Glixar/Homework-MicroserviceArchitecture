using AuthService.Domain;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Postgres;

public class PostgresDbContext : DbContext
{
    /// <summary>
    /// Контекст EF Core для работы с таблицей пользователей.
    /// </summary>
    public PostgresDbContext(DbContextOptions<PostgresDbContext> options)
        : base(options)
    {
    }
    /// <summary>
    /// Набор пользователей.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var user = modelBuilder.Entity<User>();

        user.ToTable("users");

        user.HasKey(x => x.Id);

        user.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

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