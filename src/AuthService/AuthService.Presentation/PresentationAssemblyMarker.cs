namespace AuthService.Presentation;

/// <summary>
/// Маркерный тип для подключения сборки AuthService.Presentation в MVC (ApplicationPart).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAccountsPresentation(this IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(DependencyInjection).Assembly);
        return services;
    }
}