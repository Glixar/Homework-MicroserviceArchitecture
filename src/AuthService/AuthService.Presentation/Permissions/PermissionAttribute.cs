using Microsoft.AspNetCore.Authorization;

namespace AuthService.Presentation.Permissions;

public sealed class PermissionAttribute : AuthorizeAttribute
{
    public const string PREFIX = "permission:";

    public PermissionAttribute(string code)
    {
        Code = code;
        Policy = PREFIX + code;
    }

    public string Code { get; }
}