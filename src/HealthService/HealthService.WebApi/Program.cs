namespace HealthService.WebApi;

public abstract class Program()
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        
        var app = builder.Build();

        app.MapControllers();
        
        app.Run();
    }
}