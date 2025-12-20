namespace AuthService.Infrastructure.Postgres.Maintenance;

/// <summary>
/// Контракт для выполнения миграций хранилища данных.
/// </summary>
public interface IDatabaseMigrator
{
    /// <summary>
    /// Применить миграции (если включено конфигурацией).
    /// </summary>
    Task MigrateAsync(CancellationToken cancellationToken = default);
}