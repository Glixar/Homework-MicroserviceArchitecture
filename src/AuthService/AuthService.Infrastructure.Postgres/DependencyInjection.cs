using AuthService.Application.Abstractions;
using AuthService.Contracts.Options;
using AuthService.Domain;
using AuthService.Infrastructure.Postgres.IdentityManagers;
using AuthService.Infrastructure.Postgres.IdentityValidators;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.Postgres;

public static class DependencyInjection
{
    public static IServiceCollection AddAccountsInfrastructure(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SECTION_NAME);

        services.AddTransient<ITokenProvider, JwtTokenProvider>();

        services.RegisterIdentity();
        return services;
    }

    private static void RegisterIdentity(this IServiceCollection services)
    {
        // Кастомный валидатор, который убирает требование уникальности UserName.
        // Все остальные стандартные проверки (формат, e-mail и т.д.) остаются.
        services.AddScoped<IUserValidator<User>, UserValidatorAllowDuplicateUserName>();

        services
            .AddIdentityCore<User>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                    "0123456789" +
                    "-._@+ " +
                    "абвгдеёжзийклмнопрстуфхцчшщъыьэюяАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
            })
            .AddRoles<Role>()
            .AddEntityFrameworkStores<PostgresDbContext>();

        services.AddScoped<RolePermissionManager>();
        services.AddScoped<IRefreshSessionManager, RefreshSessionManager>();
    }

    public static IServiceCollection AddPostgresInfrastructure(this IServiceCollection services)
    {
        services.AddOptions<ConnectionStringsOptions>()
            .BindConfiguration(ConnectionStringsOptions.SECTION_NAME)
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.Database),
                "Строка подключения к БД (ConnectionStrings:Database) не задана")
            .ValidateOnStart();

        services.AddDbContextPool<PostgresDbContext>((sp, opt) =>
        {
            string cs = sp.GetRequiredService<IOptionsMonitor<ConnectionStringsOptions>>()
                .CurrentValue.Database;

            opt.UseNpgsql(cs, npg =>
            {
                npg.EnableRetryOnFailure();
                npg.MigrationsHistoryTable("__EFMigrationsHistory", "accounts");
            });
            
            
            // Единый контракт именования для БД (таблицы/колонки/индексы).
            opt.UseSnakeCaseNamingConvention();

#if DEBUG
            opt.EnableDetailedErrors();
            opt.EnableSensitiveDataLogging();
#endif
        });

        // менеджеры прав и аккаунтов
        services.AddScoped<IPermissionManager, PermissionManager>();
        services.AddScoped<PermissionManager>();

        return services;
    }
}