using AuthService.Application.Users;
using AuthService.Infrastructure.Postgres;
using AuthService.Infrastructure.Postgres.Options;
using AuthService.Infrastructure.Postgres.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthService.WebApi;

/// <summary>
/// Точка входа Web API приложения.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Основной метод запуска приложения.
    /// </summary>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddEndpointsApiExplorer();

        // Конфигурация options для подключения к базе данных.
        ConfigureDatabaseOptions(builder);

        // Регистрация DbContext, который получает строку подключения из DatabaseOptions.
        ConfigureDatabase(builder);

        // Регистрация доменного сервиса работы с пользователями.
        builder.Services.AddScoped<IUserService, PostgresUserService>();

        var app = builder.Build();

        // Режим "только миграции" для Kubernetes Job.
        if (args.Contains("--migrate-only", StringComparer.OrdinalIgnoreCase))
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();

            await dbContext.Database.MigrateAsync();

            return;
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        // Для кластера Ingress обычно ходит по HTTP, редирект можно не включать.
        // app.UseHttpsRedirection();

        app.UseAuthorization();

        // Простой health-check для readiness / liveness.
        app.MapGet("/health", () => Results.Ok("OK"));

        app.MapControllers();

        await app.RunAsync();
    }

    /// <summary>
    /// Регистрирует DatabaseOptions и связывает их с конфигурацией.
    /// Ожидаются ключи вида:
    /// Database:Host, Database:Port, Database:Name, Database:Username, Database:Password.
    /// </summary>
    private static void ConfigureDatabaseOptions(WebApplicationBuilder builder)
    {
        builder.Services.Configure<DatabaseOptions>(
            builder.Configuration.GetSection(DatabaseOptions.SECTION_NAME));
    }

    /// <summary>
    /// Регистрирует PostgresDbContext с использованием DatabaseOptions.
    /// </summary>
    private static void ConfigureDatabase(WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<PostgresDbContext>(
            (serviceProvider, optionsBuilder) =>
            {
                var dbOptions = serviceProvider
                    .GetRequiredService<IOptions<DatabaseOptions>>()
                    .Value;

                var connectionString = dbOptions.BuildConnectionString();

                optionsBuilder.UseNpgsql(connectionString);
            });
    }
}