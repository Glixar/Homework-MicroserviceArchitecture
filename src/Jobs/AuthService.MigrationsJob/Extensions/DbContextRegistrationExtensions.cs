using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AuthService.MigrationsJob.Extensions
{
    internal static class DbContextRegistrationExtensions
    {
        public static IServiceCollection AddAuthDbContext(this IServiceCollection services)
        {
            services.AddDbContext<TContext>((sp, options) =>
            {
                var dataSource = sp.GetRequiredService<NpgsqlDataSource>();

                options.UseNpgsql(
                    dataSource,
                    npg =>
                    {
                        npg.EnableRetryOnFailure();
                        npg.MigrationsHistoryTable("__EFMigrationsHistory", "accounts");
                    });
                
                options.UseSnakeCaseNamingConvention();

#if DEBUG
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
#endif
            });

            services
                .AddIdentityCore<TUser>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.User.AllowedUserNameCharacters =
                        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                        "0123456789" +
                        "-._@+ " +
                        "абвгдеёжзийклмнопрстуфхцчшщъыьэюяАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
                })
                .AddRoles<TRole>()
                .AddEntityFrameworkStores<TContext>();

            return services;
        }
    }
}