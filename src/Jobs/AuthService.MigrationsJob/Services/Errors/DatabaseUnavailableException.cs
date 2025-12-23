namespace AuthService.MigrationsJob.Services.Errors
{
    /// <summary>
    /// Ошибка недоступности БД после серии попыток подключения.
    /// </summary>
    internal sealed class DatabaseUnavailableException : Exception
    {
        public DatabaseUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}