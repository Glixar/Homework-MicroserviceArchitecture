namespace AuthService.Infrastructure.Postgres.Options;

public sealed class DatabaseMigrationsOptions
{
    public const string SECTION_NAME = "DatabaseMigrations";

    /// <summary>
    /// Применять EF Core миграции при старте сервиса.
    /// Рекомендуется включать только для локальной разработки/CI.
    /// В проде безопаснее выполнять миграции отдельным Job/InitContainer.
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// Максимальное количество попыток применения миграций.
    /// Полезно, когда Postgres ещё не готов (Docker/Kubernetes).
    /// </summary>
    public int MaxAttempts { get; init; } = 10;

    /// <summary>
    /// Базовая задержка (в секундах) для экспоненциального backoff.
    /// </summary>
    public int BaseDelaySeconds { get; init; } = 2;

    /// <summary>
    /// Максимальная задержка (в секундах) для экспоненциального backoff.
    /// </summary>
    public int MaxDelaySeconds { get; init; } = 30;

    /// <summary>
    /// Ключ pg_advisory_lock для сериализации миграций между несколькими инстансами сервиса.
    /// </summary>
    public long AdvisoryLockId { get; init; } = 6753500293685907738;
}