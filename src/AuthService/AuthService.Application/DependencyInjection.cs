using AuthService.Application.Commands.Accounts.Handlers;
using AuthService.Application.Commands.AdminPanel.Handlers;
using AuthService.Application.Commands.Auth.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Командные хэндлеры авторизации
        services.AddScoped<LoginHandler>();
        services.AddScoped<LogoutHandler>();
        services.AddScoped<RefreshTokensHandler>();
        services.AddScoped<RegisterHandler>();
        services.AddScoped<CheckEmailHandler>();

        // Командные хэндлеры личного кабинета пользователя
        services.AddScoped<ChangeEmailHandler>();
        services.AddScoped<ChangePasswordHandler>();
        services.AddScoped<DeleteMyProfileHandler>();
        services.AddScoped<GetMyProfileHandler>();
        services.AddScoped<UpdateMyProfileHandler>();

        // Командные хэндлеры админ-панели
        services.AddScoped<GetAllUsersHandler>();
        services.AddScoped<GetUserByIdHandler>();
        services.AddScoped<GetUserByEmailHandler>();
        services.AddScoped<CreateUserHandler>();
        services.AddScoped<UpdateUserHandler>();
        services.AddScoped<DeleteUserByIdHandler>();
        services.AddScoped<RestoreUserHandler>();

        return services;
    }
}