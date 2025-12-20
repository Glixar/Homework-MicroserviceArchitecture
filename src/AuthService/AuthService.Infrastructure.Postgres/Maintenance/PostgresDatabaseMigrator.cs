using AuthService.Infrastructure.Postgres.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.Postgres.Maintenance;

/// <summary>
/// Исполнитель миграций EF Core для PostgreSQL.
/// </summary>
internal sealed class PostgresDatabaseMigrator : IDatabaseMigrator
{
    private readonly PostgresDbContext _dbContext;
    private readonly DatabaseMigrationsOptions _options;
    private readonly ILogger<PostgresDatabaseMigrator> _logger;

    public PostgresDatabaseMigrator(
        PostgresDbContext dbContext,
        IOptions<DatabaseMigrationsOptions> options,
        ILogger<PostgresDatabaseMigrator> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Автомиграции выключены (DatabaseMigrations:Enabled = false). Пропускаю применение миграций.");
            return;
        }

        for (var attempt = 1; attempt <= _options.MaxAttempts; attempt++)
        {
            try
            {
                _logger.LogInformation(
                    "Проверяю и применяю миграции EF Core (попытка {Attempt}/{MaxAttempts})...",
                    attempt,
                    _options.MaxAttempts);

                await _dbContext.Database.OpenConnectionAsync(cancellationToken);

                try
                {
                    await AcquireAdvisoryLockAsync(cancellationToken);

                    try
                    {
                        await _dbContext.Database.MigrateAsync(cancellationToken);
                    }
                    finally
                    {
                        await ReleaseAdvisoryLockSafeAsync(cancellationToken);
                    }
                }
                finally
                {
                    await _dbContext.Database.CloseConnectionAsync();
                }

                _logger.LogInformation("Миграции применены успешно.");
                return;
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                var delay = CalculateDelay(
                    attempt,
                    TimeSpan.FromSeconds(_options.BaseDelaySeconds),
                    TimeSpan.FromSeconds(_options.MaxDelaySeconds));

                _logger.LogWarning(
                    ex,
                    "Не удалось применить миграции из-за временной ошибки. Повтор через {DelaySeconds} сек. (попытка {Attempt}/{MaxAttempts}).",
                    delay.TotalSeconds,
                    attempt,
                    _options.MaxAttempts);

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"Не удалось применить миграции EF Core к Postgres за {_options.MaxAttempts} попыток. " +
            $"Проверь доступность БД и параметры секций '{DatabaseOptions.SECTION_NAME}' и '{DatabaseMigrationsOptions.SECTION_NAME}'.");
    }

    private async Task AcquireAdvisoryLockAsync(CancellationToken cancellationToken)
    {
        // Advisory-lock берём на уровне сессии (connection-level), чтобы:
        // - миграции не выполнялись параллельно на нескольких репликах;
        // - при одновременном старте инстансов остальные подождали.
        await _dbContext.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_lock({0});",
            new object[] { _options.AdvisoryLockId },
            cancellationToken);
    }

    private async Task ReleaseAdvisoryLockSafeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_unlock({0});",
                new object[] { _options.AdvisoryLockId },
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Нельзя ломать старт приложения из-за проблемы с unlock: если соединение уже умерло,
            // unlock и так “не нужен”, сессия завершится и PostgreSQL снимет lock автоматически.
            _logger.LogWarning(ex, "Не удалось снять pg_advisory_lock. Возможно, соединение к БД уже было разорвано.");
        }
    }

    private static bool IsTransient(Exception ex)
    {
        return ex is TimeoutException
            or System.Data.Common.DbException
            or DbUpdateException
            or InvalidOperationException;
    }

    private static TimeSpan CalculateDelay(int attempt, TimeSpan baseDelay, TimeSpan maxDelay)
    {
        // Экспоненциальный backoff: 2s, 4s, 8s... с ограничением MaxDelay
        var seconds = baseDelay.TotalSeconds * Math.Pow(2, attempt - 1);
        var delay = TimeSpan.FromSeconds(seconds);

        return delay > maxDelay ? maxDelay : delay;
    }
}