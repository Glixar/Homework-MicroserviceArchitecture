using AuthService.Application.Users;
using AuthService.Infrastructure.Postgres.Maintenance;
using AuthService.Infrastructure.Postgres.Options;
using AuthService.Infrastructure.Postgres.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.Postgres;

public static class DependencyInjection
{
    public static IServiceCollection AddPostgresInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SECTION_NAME))
            .Validate(
                o =>
                    !string.IsNullOrWhiteSpace(o.Host) &&
                    !string.IsNullOrWhiteSpace(o.Database) &&
                    !string.IsNullOrWhiteSpace(o.Username) &&
                    !string.IsNullOrWhiteSpace(o.Password) &&
                    o.Port is >= 1 and <= 65535,
                "Секция 'Database' в конфигурации заполнена некорректно. Проверь Database:Host/Port/Database/Username/Password.")
            .ValidateOnStart();

        services
            .AddOptions<DatabaseMigrationsOptions>()
            .Bind(configuration.GetSection(DatabaseMigrationsOptions.SECTION_NAME))
            .Validate(
                o =>
                    o.MaxAttempts is >= 1 and <= 100 &&
                    o.BaseDelaySeconds is >= 1 and <= 600 &&
                    o.MaxDelaySeconds is >= 1 and <= 600 &&
                    o.MaxDelaySeconds >= o.BaseDelaySeconds,
                "Секция 'DatabaseMigrations' в конфигурации заполнена некорректно. Проверь DatabaseMigrations:Enabled/MaxAttempts/BaseDelaySeconds/MaxDelaySeconds.")
            .ValidateOnStart();

        services.AddDbContext<PostgresDbContext>((sp, options) =>
        {
            var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseNpgsql(dbOptions.BuildConnectionString());
        });

        services.AddScoped<IUserService, PostgresUserService>();
        services.AddScoped<IDatabaseMigrator, PostgresDatabaseMigrator>();

        return services;
    }
}