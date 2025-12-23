using System.Text;
using AuthService.MigrationsJob.Extensions;
using AuthService.MigrationsJob.Options;
using AuthService.MigrationsJob.Services;
using AuthService.MigrationsJob.Services.Errors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthService.MigrationsJob
{
    /// <summary>
    /// Exit-коды:
    /// 0  — успех
    /// 1  — ошибка
    /// 2  — ошибка конфигурации/опций
    /// 10 — провал миграций
    /// 11 — провал сидирования
    /// 12 — БД недоступна после ретраев
    /// </summary>
    public sealed class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Установка кодировки консоли
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            builder.Logging.ClearProviders();
            builder.Logging.AddSimpleConsole(o =>
            {
                o.TimestampFormat = "HH:mm:ss ";
                o.SingleLine = true;
            });

            builder.Configuration
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile(
                    $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json",
                    optional: true)
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables();

            builder.Services.AddAuthMigrationsOptions(builder.Configuration);

            builder.Services.AddNpgsqlDataSource();
            builder.Services.AddAuthDbContext();

            builder.Services.AddScoped<Migrator>();
            builder.Services.AddScoped<Seeder>();

            using var app = builder.Build();
            var log = app.Services.GetRequiredService<ILogger<Program>>();

            try
            {
                var migrationsOptions = app.Services
                    .GetRequiredService<IOptions<MigrationsOptions>>()
                    .Value;

                bool doMigrate = migrationsOptions.Migrate;
                bool doSeed = migrationsOptions.Seed;

                log.LogInformation(
                    "Старт джобы миграций. Migrations:Migrate={Migrate}, Migrations:Seed={Seed}",
                    doMigrate,
                    doSeed);

                var migrator = app.Services.GetRequiredService<Migrator>();

                if (doMigrate || doSeed)
                {
                    try
                    {
                        await migrator.EnsureDatabaseAvailableAsync();
                    }
                    catch (DatabaseUnavailableException ex)
                    {
                        log.LogCritical(ex, "База данных недоступна после ретраев.");
                        return ExitCodes.DatabaseUnavailable;
                    }
                }

                if (doMigrate)
                {
                    try
                    {
                        await migrator.ApplyMigrationsAsync();
                    }
                    catch (MigrationFailedException ex)
                    {
                        log.LogCritical(ex, "Провал миграций: {Context}", ex.ContextType);
                        return ExitCodes.MigrationFailed;
                    }
                }
                else
                {
                    log.LogWarning("Миграции пропущены (Migrations:Migrate = false).");
                }

                if (doSeed)
                {
                    try
                    {
                        using var scope = app.Services.CreateScope();
                        var seeder = scope.ServiceProvider.GetRequiredService<Seeder>();
                        await seeder.RunAsync(CancellationToken.None);
                    }
                    catch (SeedingFailedException ex)
                    {
                        log.LogCritical(ex, "Провал сидирования.");
                        return ExitCodes.SeedingFailed;
                    }
                }
                else
                {
                    log.LogInformation("Сидирование пропущено (Migrations:Seed = false).");
                }

                log.LogInformation("Джоба завершилась успешно.");
                return ExitCodes.Ok;
            }
            catch (OptionsValidationException ex)
            {
                Console.Error.WriteLine($"[ОШИБКА] Опции: {ex.Message}");
                foreach (var failure in ex.Failures)
                {
                    Console.Error.WriteLine($" - {failure}");
                }

                return ExitCodes.OptionsValidationFailed;
            }
            catch (Exception ex)
            {
                log.LogCritical(ex, "Критическая ошибка: {Message}", ex.Message);
                return ExitCodes.GeneralError;
            }
        }
    }
}