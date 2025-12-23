namespace AuthService.MigrationsJob.Services.Errors
{
    /// <summary>
    /// Ошибка выполнения сидирования (роли, пермишены, администратор).
    /// </summary>
    internal sealed class SeedingFailedException : Exception
    {
        public SeedingFailedException(string message)
            : base(message)
        {
        }

        public SeedingFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}