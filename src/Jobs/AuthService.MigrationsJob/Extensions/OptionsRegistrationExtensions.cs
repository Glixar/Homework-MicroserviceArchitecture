using AuthService.MigrationsJob.Options;
using AuthService.MigrationsJob.Options.Validators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AuthService.MigrationsJob.Extensions
{
    internal static class OptionsRegistrationExtensions
    {
        public static IServiceCollection AddAuthMigrationsOptions(this IServiceCollection services, IConfiguration cfg)
        {
            // ---------- Options ----------

            services.AddOptions<RolePermissionOptions>()
                .Bind(cfg.GetSection(RolePermissionOptions.SECTION_NAME))
                .ValidateDataAnnotations()
                .Validate(o => o.Roles is { Count: > 0 }, "Должен быть хотя бы один роль-мэппинг")
                .ValidateOnStart();

            services.AddOptions<DefaultAdministratorOptions>()
                .Bind(cfg.GetSection(DefaultAdministratorOptions.SECTION_NAME))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<DatabaseOptions>()
                .Bind(cfg.GetSection(DatabaseOptions.SECTION_NAME))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<RetryOptions>()
                .Bind(cfg.GetSection(RetryOptions.SECTION_NAME))
                .ValidateOnStart();

            services.AddOptions<MigrationsOptions>()
                .Bind(cfg.GetSection(MigrationsOptions.SECTION_NAME))
                .ValidateOnStart();

            // ---------- Validators ----------

            services.AddSingleton<IValidateOptions<RolePermissionOptions>, RolePermissionOptionsValidator>();
            services.AddSingleton<IValidateOptions<DefaultAdministratorOptions>, DefaultAdministratorOptionsValidator>();

            return services;
        }
    }
}