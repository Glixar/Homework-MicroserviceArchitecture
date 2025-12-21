using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Presentation;

public static class DependencyInjection
{
    public static IServiceCollection AddAccountsPresentation(this IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(DependencyInjection).Assembly);
        return services;
    }
}