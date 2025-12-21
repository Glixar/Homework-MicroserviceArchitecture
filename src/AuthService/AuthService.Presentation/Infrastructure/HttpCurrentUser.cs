using System.Security.Claims;
using AuthService.Application.Abstractions;
using AuthService.Contracts.Models;
using Microsoft.AspNetCore.Http;

namespace AuthService.Presentation.Infrastructure;

public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;

    public HttpCurrentUser(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? User => _http.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId =>
        GuidTry(User?.FindFirstValue(CustomClaims.Id));

    public string? Email => User?.FindFirstValue(CustomClaims.Email);

    public IReadOnlyCollection<string> Roles =>
        User?.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray()
        ?? Array.Empty<string>();

    public IReadOnlyCollection<string> Permissions =>
        User?.Claims.Where(c => c.Type == CustomClaims.Permission).Select(c => c.Value).ToArray()
        ?? Array.Empty<string>();

    public Guid? SessionId =>
        GuidTry(User?.FindFirstValue(CustomClaims.SessionId));

    public string? IpAddress => _http.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    public string? UserAgent => _http.HttpContext?.Request?.Headers["User-Agent"].ToString();

    private static Guid? GuidTry(string? value)
        => Guid.TryParse(value, out var id) ? id : null;
}