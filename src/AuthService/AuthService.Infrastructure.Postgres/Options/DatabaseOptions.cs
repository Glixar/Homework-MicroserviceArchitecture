using System.ComponentModel.DataAnnotations;

namespace AuthService.Infrastructure.Postgres.Options;

/// <summary>
/// Настройки подключения к базе данных PostgreSQL.
/// </summary>
public sealed class DatabaseOptions
{
    /// <summary>
    /// Имя секции конфигурации.
    /// </summary>
    public const string SECTION_NAME = "Database";

    /// <summary>
    /// Хост PostgreSQL.
    /// </summary>
    [Required(ErrorMessage = "Хост базы данных обязателен.")]
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Порт PostgreSQL.
    /// </summary>
    public int Port { get; set; } = 5432;

    /// <summary>
    /// Имя базы данных.
    /// </summary>
    [Required(ErrorMessage = "Имя базы данных обязательно.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Имя пользователя.
    /// </summary>
    [Required(ErrorMessage = "Имя пользователя базы данных обязательно.")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Пароль пользователя.
    /// </summary>
    [Required(ErrorMessage = "Пароль пользователя базы данных обязателен.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Сформировать строку подключения Npgsql.
    /// </summary>
    public string BuildConnectionString()
    {
        if (string.IsNullOrWhiteSpace(Host) ||
            string.IsNullOrWhiteSpace(Name) ||
            string.IsNullOrWhiteSpace(Username) ||
            string.IsNullOrWhiteSpace(Password))
        {
            throw new InvalidOperationException(
                "Параметры подключения к базе данных заданы некорректно (секция Database).");
        }

        return $"Host={Host};Port={Port};Database={Name};Username={Username};Password={Password}";
    }
}