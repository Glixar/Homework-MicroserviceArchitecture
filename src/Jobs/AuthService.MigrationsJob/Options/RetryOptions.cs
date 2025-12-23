namespace AuthService.MigrationsJob.Options
{
    public sealed class RetryOptions
    {
        public const string SECTION_NAME = "Retry";

        public int Attempts { get; init; } = 12;

        public int BaseDelaySeconds { get; init; } = 2;

        public int MaxBackoffSeconds { get; init; } = 10;
    }
}