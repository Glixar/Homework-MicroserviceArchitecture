using AuthService.Contracts.Models;
using Microsoft.AspNetCore.Authorization;

namespace AuthService.Presentation.Permissions;

/// <summary>
/// Обработчик проверки пермишена: ищет требуемый код в клеймах пользователя.
/// </summary>
public sealed class PermissionRequirementHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        bool has = context.User
            .FindAll(CustomClaims.Permission)
            .Any(c => string.Equals(c.Value, requirement.Code, StringComparison.OrdinalIgnoreCase));

        if (has)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}