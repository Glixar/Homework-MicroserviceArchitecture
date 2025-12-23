using System.ComponentModel.DataAnnotations;

namespace AuthService.MigrationsJob.Options
{
    public sealed class DefaultAdministratorOptions
    {
        public const string SECTION_NAME = "DefaultAdministrator";

        public bool Apply { get; init; } = false;

        [Required, MinLength(3)]
        public string? UserName { get; init; }

        [Required, EmailAddress]
        public string? Email { get; init; }

        [Required]
        public string? Password { get; init; }
    }
}