using System.Security.Claims;
using AuthService.Contracts.Models;
using AuthService.Domain;
using AuthService.Infrastructure.Postgres;
using AuthService.MigrationsJob.Options;
using AuthService.MigrationsJob.Services.Errors;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthService.MigrationsJob.Services
{
    internal sealed class Seeder
    {
        private readonly PostgresDbContext _dbContext;
        private readonly RoleManager<Role> _roleManager;
        private readonly UserManager<User> _userManager;
        private readonly IOptions<RolePermissionOptions> _rolePermissionOptions;
        private readonly IOptions<DefaultAdministratorOptions> _adminOptions;
        private readonly ILogger<Seeder> _logger;

        public Seeder(
            PostgresDbContext dbContext,
            RoleManager<Role> roleManager,
            UserManager<User> userManager,
            IOptions<RolePermissionOptions> rolePermissionOptions,
            IOptions<DefaultAdministratorOptions> adminOptions,
            ILogger<Seeder> logger)
        {
            _dbContext = dbContext;
            _roleManager = roleManager;
            _userManager = userManager;
            _rolePermissionOptions = rolePermissionOptions;
            _adminOptions = adminOptions;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await EnsureRolesAsync(cancellationToken);

            DefaultAdministratorOptions adminCfg = _adminOptions.Value;
            if (!adminCfg.Apply)
            {
                _logger.LogInformation(
                    "DefaultAdministrator.Apply = false — создание администратора пропущено.");
                return;
            }

            await EnsureAdminAsync(cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureRolesAsync(CancellationToken cancellationToken)
        {
            RolePermissionOptions cfg = _rolePermissionOptions.Value;

            List<string> allPermissionCodes = new();

            if (cfg.Permissions is not null)
            {
                foreach ((string _, List<string> groupPermissions) in cfg.Permissions)
                {
                    if (groupPermissions is null)
                    {
                        continue;
                    }

                    allPermissionCodes.AddRange(groupPermissions);
                }
            }

            foreach ((string _, List<string> rolePermissions) in cfg.Roles)
            {
                if (rolePermissions is null)
                {
                    continue;
                }

                allPermissionCodes.AddRange(rolePermissions);
            }

            HashSet<string> distinctPermissionCodes = allPermissionCodes
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (string code in distinctPermissionCodes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool exists = await _dbContext.Permissions
                    .AnyAsync(p => p.Code == code, cancellationToken);

                if (!exists)
                {
                    _dbContext.Permissions.Add(
                        new Permission
                        {
                            Id = Guid.NewGuid(),
                            Code = code
                        });

                    _logger.LogInformation(
                        "Добавлено право '{Permission}' в справочник Permissions.",
                        code);
                }
            }

            foreach ((string roleName, List<string> permissions) in cfg.Roles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Role? role = await _roleManager.FindByNameAsync(roleName);
                if (role is null)
                {
                    role = CreateInstance<Role>("роль", roleName);
                    role.Name = roleName;
                    role.NormalizedName = _roleManager.NormalizeKey(roleName);

                    IdentityResult createResult = await _roleManager.CreateAsync(role);
                    if (!createResult.Succeeded)
                    {
                        throw new SeedingFailedException(
                            $"Ошибка создания роли '{roleName}': {FormatIdentityErrors(createResult.Errors)}");
                    }

                    _logger.LogInformation(
                        "Роль '{Role}' создана.",
                        roleName);
                }

                IList<Claim> existingClaims = await _roleManager.GetClaimsAsync(role);
                HashSet<string> existingPermissionClaims = existingClaims
                    .Where(c => c.Type == CustomClaims.Permission)
                    .Select(c => c.Value)
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => v.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                HashSet<Guid> existingPermissionIds = await _dbContext.RolePermissions
                    .Where(rp => rp.RoleId == role.Id)
                    .Select(rp => rp.PermissionId)
                    .ToHashSetAsync(cancellationToken);

                foreach (string permission in permissions)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrWhiteSpace(permission))
                    {
                        continue;
                    }

                    string permissionCode = permission.Trim();

                    if (!existingPermissionClaims.Contains(permissionCode))
                    {
                        IdentityResult addClaimResult = await _roleManager.AddClaimAsync(
                            role,
                            new Claim(CustomClaims.Permission, permissionCode));

                        if (!addClaimResult.Succeeded)
                        {
                            throw new SeedingFailedException(
                                $"Ошибка добавления права '{permissionCode}' в роль '{roleName}': {FormatIdentityErrors(addClaimResult.Errors)}");
                        }

                        _logger.LogInformation(
                            "Роли '{Role}' добавлено право '{Permission}' (клейм).",
                            roleName,
                            permissionCode);
                    }

                    Permission? permissionEntity = await _dbContext.Permissions
                        .FirstOrDefaultAsync(p => p.Code == permissionCode, cancellationToken);

                    if (permissionEntity is null)
                    {
                        _logger.LogWarning(
                            "Право '{Permission}' не найдено в таблице Permissions при привязке к роли '{Role}'.",
                            permissionCode,
                            roleName);
                        continue;
                    }

                    if (!existingPermissionIds.Contains(permissionEntity.Id))
                    {
                        _dbContext.RolePermissions.Add(
                            new RolePermission
                            {
                                RoleId = role.Id,
                                PermissionId = permissionEntity.Id
                            });

                        _logger.LogInformation(
                            "К роли '{Role}' привязано право '{Permission}' через RolePermissions.",
                            roleName,
                            permissionCode);
                    }
                }
            }
        }

        private async Task EnsureAdminAsync(CancellationToken cancellationToken)
        {
            DefaultAdministratorOptions cfg = _adminOptions.Value;

            if (!cfg.Apply)
            {
                _logger.LogInformation("Создание администратора отключено в конфигурации.");
                return;
            }

            if (string.IsNullOrWhiteSpace(cfg.UserName) ||
                string.IsNullOrWhiteSpace(cfg.Email) ||
                string.IsNullOrWhiteSpace(cfg.Password))
            {
                throw new SeedingFailedException(
                    "Настройки DefaultAdministrator заданы некорректно (UserName/Email/Password пустые).");
            }

            User? user = await _userManager.FindByNameAsync(cfg.UserName);
            if (user is null)
            {
                User domainUser = User.CreateUser(
                    cfg.UserName,
                    cfg.Email,
                    "Администратор по умолчанию");

                IdentityResult createResult = await _userManager.CreateAsync(domainUser, cfg.Password);
                if (!createResult.Succeeded)
                {
                    throw new SeedingFailedException(
                        $"Ошибка создания администратора '{cfg.UserName}': {FormatIdentityErrors(createResult.Errors)}");
                }

                _logger.LogInformation(
                    "Пользователь-администратор '{UserName}' создан.",
                    cfg.UserName);

                user = domainUser;
            }
            else
            {
                _logger.LogInformation(
                    "Пользователь-администратор '{UserName}' уже существует, пропускаем создание.",
                    cfg.UserName);
            }

            if (_rolePermissionOptions.Value.Roles.ContainsKey("ADMIN"))
            {
                if (!await _userManager.IsInRoleAsync(user, "ADMIN"))
                {
                    IdentityResult addRoleResult = await _userManager.AddToRoleAsync(user, "ADMIN");
                    if (!addRoleResult.Succeeded)
                    {
                        throw new SeedingFailedException(
                            $"Не удалось добавить администратора '{cfg.UserName}' в роль 'ADMIN': {FormatIdentityErrors(addRoleResult.Errors)}");
                    }

                    _logger.LogInformation(
                        "Пользователь '{UserName}' добавлен в роль 'ADMIN'.",
                        cfg.UserName);
                }
            }
            else
            {
                _logger.LogWarning(
                    "Роль 'ADMIN' не описана в RolePermission.Roles — администратор будет без привязки к роли.");
            }
        }

        private static T CreateInstance<T>(string entityName, string key)
            where T : class
        {
            object? instance = Activator.CreateInstance(typeof(T), nonPublic: true);
            if (instance is T typed)
            {
                return typed;
            }

            throw new SeedingFailedException(
                $"Не удалось создать {entityName} '{key}'. " +
                $"Убедитесь, что у типа {typeof(T).FullName} есть параметрless конструктор (может быть непубличным).");
        }

        private static string FormatIdentityErrors(IEnumerable<IdentityError> errors)
        {
            return string.Join("; ", errors.Select(e => $"{e.Code}: {e.Description}"));
        }
    }
}