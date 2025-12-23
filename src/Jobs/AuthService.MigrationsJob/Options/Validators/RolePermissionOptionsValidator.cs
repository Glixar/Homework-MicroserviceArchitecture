using Microsoft.Extensions.Options;

namespace AuthService.MigrationsJob.Options.Validators
{
    public sealed class RolePermissionOptionsValidator : IValidateOptions<RolePermissionOptions>
    {
        public ValidateOptionsResult Validate(string? name, RolePermissionOptions options)
        {
            if (options.Permissions is null || options.Permissions.Count == 0)
            {
                return ValidateOptionsResult.Fail("RolePermission.Permissions: секция обязательна и не может быть пустой.");
            }

            if (options.Roles is null || options.Roles.Count == 0)
            {
                return ValidateOptionsResult.Fail("RolePermission.Roles: секция обязательна и не может быть пустой.");
            }

            var catalog = new HashSet<string>(StringComparer.Ordinal);
            foreach (var list in options.Permissions.Values)
            {
                if (list is null)
                {
                    continue;
                }

                foreach (var p in list)
                {
                    if (!string.IsNullOrWhiteSpace(p))
                    {
                        catalog.Add(p);
                    }
                }
            }

            var missing = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var role in options.Roles.Keys)
            {
                var perms = options.Roles[role] ?? new List<string>();
                foreach (var p in perms)
                {
                    if (string.IsNullOrWhiteSpace(p) || !catalog.Contains(p))
                    {
                        missing.Add(p ?? string.Empty);
                    }
                }
            }

            if (missing.Count > 0)
            {
                var details = string.Join(", ", missing.Where(x => !string.IsNullOrWhiteSpace(x)));
                return ValidateOptionsResult.Fail($"RolePermission: отсутствуют описания в Permissions для пермишенов из Roles: {details}");
            }

            return ValidateOptionsResult.Success;
        }
    }
}