using System.ComponentModel.DataAnnotations;

namespace AuthService.MigrationsJob.Options
{
    public sealed class DatabaseOptions
    {
        public const string SECTION_NAME = "ConnectionStrings";

        [Required]
        public string? Database { get; init; }
    }
}