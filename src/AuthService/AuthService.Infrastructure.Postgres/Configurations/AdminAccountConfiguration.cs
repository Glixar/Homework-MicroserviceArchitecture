using AuthService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthService.Infrastructure.Postgres.Configurations;

public sealed class AdminAccountConfiguration : IEntityTypeConfiguration<AdminAccount>
{
    public void Configure(EntityTypeBuilder<AdminAccount> b)
    {
        b.ToTable("admin_accounts");
        b.HasKey(x => x.Id);

        b.HasOne(a => a.User)
            .WithOne()
            .HasForeignKey<AdminAccount>(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(a => a.UserId).IsUnique();

        b.Property(a => a.FullName)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnName("full_name");
    }
}