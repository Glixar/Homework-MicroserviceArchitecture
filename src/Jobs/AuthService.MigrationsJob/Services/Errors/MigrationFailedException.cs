namespace AuthService.MigrationsJob.Services.Errors
{
    /// <summary>
    /// Ошибка применения миграций для конкретного DbContext.
    /// </summary>
    internal sealed class MigrationFailedException : Exception
    {
        /// <summary>
        /// Полное имя типа DbContext, для которого упали миграции.
        /// </summary>
        public string ContextType { get; }

        public MigrationFailedException(string contextType, string message, Exception innerException)
            : base(message, innerException)
        {
            ContextType = contextType;
        }
    }
}