using System.Reflection;
using AuthService.MigrationsJob.Options;
using AuthService.MigrationsJob.Services.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Polly;

namespace AuthService.MigrationsJob.Services
{
    public sealed class Migrator
    {
        private readonly ILogger<Migrator> _log;
        private readonly NpgsqlDataSource _dataSource;
        private readonly RetryOptions _retry;

        public Migrator(ILogger<Migrator> log, NpgsqlDataSource dataSource, IOptions<RetryOptions> retry)
        {
            _log = log;
            _dataSource = dataSource;
            _retry = retry.Value;
        }

        public async Task EnsureDatabaseAvailableAsync(CancellationToken cancellationToken = default)
        {
            var attempts = _retry.Attempts;
            var baseDelaySeconds = _retry.BaseDelaySeconds;
            var maxBackoffSeconds = _retry.MaxBackoffSeconds;

            Exception? lastException = null;
            TimeSpan lastDelay = TimeSpan.Zero;

            var policy = Policy
                .Handle<NpgsqlException>()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    attempts,
                    i =>
                    {
                        var delay = TimeSpan.FromSeconds(
                            Math.Min(maxBackoffSeconds, baseDelaySeconds * i));
                        return delay;
                    },
                    (ex, delay, attempt, _) =>
                    {
                        lastException = ex;
                        lastDelay = delay;

                        _log.LogWarning(
                            ex,
                            "[{Attempt}/{Attempts}] Не удалось подключиться к БД, повтор через {Delay}.",
                            attempt,
                            attempts,
                            delay);
                    });

            try
            {
                await policy.ExecuteAsync(
                    async ct =>
                    {
                        await using var con = await _dataSource.OpenConnectionAsync(ct);
                        await using var cmd = new NpgsqlCommand("SELECT 1", con);
                        _ = await cmd.ExecuteScalarAsync(ct);
                    },
                    cancellationToken);
            }
            catch (Exception ex) when (ex is NpgsqlException or TimeoutException)
            {
                throw new DatabaseUnavailableException(
                    "База данных недоступна после серии ретраев.",
                    lastException ?? ex);
            }
        }

        public async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
        {
            await EnsureDatabaseAvailableAsync(cancellationToken);

            var ctxType = typeof(TContext);
            EnsureReferencedAssembliesLoaded();

            _log.LogInformation("Применяю миграции для: {Type}", ctxType.FullName);

            try
            {
                await using var ctx = CreateDbContext(ctxType, _dataSource);
                await ctx.Database.MigrateAsync(cancellationToken);
                _log.LogInformation("Готово: {Type}", ctxType.FullName);
            }
            catch (Exception ex)
            {
                var name = ctxType.FullName ?? ctxType.Name;
                throw new MigrationFailedException(name, $"Сбой миграций для контекста {name}.", ex);
            }
        }

        private static DbContext CreateDbContext(Type ctxType, NpgsqlDataSource dataSource)
        {
            var builderGenericType = typeof(DbContextOptionsBuilder<>).MakeGenericType(ctxType);
            var builderObj = Activator.CreateInstance(builderGenericType);

            if (builderObj is not DbContextOptionsBuilder builder)
            {
                throw new InvalidOperationException(
                    $"Не удалось создать DbContextOptionsBuilder<{ctxType.Name}>.");
            }

            builder.UseNpgsql(
                dataSource,
                npg =>
                {
                    npg.EnableRetryOnFailure();
                    npg.MigrationsHistoryTable("__EFMigrationsHistory", "accounts");
                });
            
            builder.UseSnakeCaseNamingConvention();
#if DEBUG
            builder.EnableDetailedErrors();
            builder.EnableSensitiveDataLogging();
#endif

            var options = builder.Options;

            var context = (DbContext?)Activator.CreateInstance(ctxType, options);
            if (context is null)
            {
                throw new InvalidOperationException(
                    $"Не удалось создать DbContext для типа {ctxType.FullName}.");
            }

            return context;
        }

        private static void EnsureReferencedAssembliesLoaded()
        {
            var entry = Assembly.GetEntryAssembly();
            if (entry == null)
            {
                return;
            }

            foreach (var name in entry.GetReferencedAssemblies())
            {
                try
                {
                    _ = Assembly.Load(name);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}