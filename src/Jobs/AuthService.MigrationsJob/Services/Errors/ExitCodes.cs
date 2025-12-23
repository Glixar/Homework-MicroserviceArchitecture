namespace AuthService.MigrationsJob.Services.Errors
{
    /// <summary>
    /// Коды завершения джобы миграций.
    /// </summary>
    internal static class ExitCodes
    {
        public const int Ok = 0;
        public const int GeneralError = 1;
        public const int OptionsValidationFailed = 2;

        public const int MigrationFailed = 10;
        public const int SeedingFailed = 11;
        public const int DatabaseUnavailable = 12;
    }
}