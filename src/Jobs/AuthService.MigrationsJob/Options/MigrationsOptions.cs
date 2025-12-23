namespace AuthService.MigrationsJob.Options
{
    public sealed class MigrationsOptions
    {
        public const string SECTION_NAME = "Migrations";

        public bool Migrate { get; init; } = true;

        public bool Seed { get; init; } = false;
    }
}