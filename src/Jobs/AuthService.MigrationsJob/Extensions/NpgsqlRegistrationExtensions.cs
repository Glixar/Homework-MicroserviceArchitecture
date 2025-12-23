using AuthService.MigrationsJob.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AuthService.MigrationsJob.Extensions
{
    internal static class NpgsqlRegistrationExtensions
    {
        public static IServiceCollection AddNpgsqlDataSource(this IServiceCollection services)
        {
            services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("NpgsqlDataSource");
                var db = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;

                if (string.IsNullOrWhiteSpace(db.Database))
                {
                    throw new InvalidOperationException("Опция ConnectionStrings:Database не задана.");
                }

                var csb = new NpgsqlConnectionStringBuilder(db.Database);
                if (csb.CommandTimeout == 0)
                {
                    csb.CommandTimeout = 180;
                }

                var dsb = new NpgsqlDataSourceBuilder(csb.ConnectionString);
                var dataSource = dsb.Build();

                logger.LogInformation("NpgsqlDataSource готов. CommandTimeout={Timeout}", csb.CommandTimeout);
                return dataSource;
            });

            return services;
        }
    }
}