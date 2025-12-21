using Microsoft.AspNetCore.Authorization;

namespace AuthService.Presentation.Permissions;

public sealed record PermissionRequirement(string Code) : IAuthorizationRequirement;