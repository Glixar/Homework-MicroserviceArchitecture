using System.ComponentModel.DataAnnotations;

namespace AuthService.MigrationsJob.Options
{
    public sealed class RolePermissionOptions
    {
        public const string SECTION_NAME = "RolePermission";

        [Required]
        public RolePermissionPermissions? Permissions { get; init; }

        [Required]
        public RolePermissionRoles Roles { get; init; } = new RolePermissionRoles();
    }

    public sealed class RolePermissionPermissions : Dictionary<string, List<string>> { }

    public sealed class RolePermissionRoles : Dictionary<string, List<string>> { }
}