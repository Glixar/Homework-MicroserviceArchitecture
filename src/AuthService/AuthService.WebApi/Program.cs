using AuthService.Infrastructure.Postgres;
using AuthService.Infrastructure.Postgres.Maintenance;
using AuthService.Presentation;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.WebApi;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        // Инициализация хоста приложения
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });

        // Отключаем авто-400 от MVC, используем свою валидацию через фильтры/мидлвари
        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        builder.Services.AddAccountsPresentation();
        
        builder.Services.AddPostgresInfrastructure(builder.Configuration);

        builder.Services.AddOpenApi();
        var app = builder.Build();
        
        // Автомиграции БД (опционально, включается через DatabaseMigrations:Enabled = true).
        using (var scope = app.Services.CreateScope())
        {
            var migrator = scope.ServiceProvider.GetRequiredService<IDatabaseMigrator>();
            await migrator.MigrateAsync();
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(opts =>
            {
                opts.SwaggerEndpoint("/openapi/v1.json", "AuthService API");
            });
        }
        
        app.UseHttpsRedirection();

        app.MapControllers();

        app.Run();
    }
}