namespace AuthService.Application.Abstractions;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    string? Email { get; }

    IReadOnlyCollection<string> Roles { get; }

    IReadOnlyCollection<string> Permissions { get; }

    Guid? SessionId { get; }

    // Техническая информация
    string? IpAddress { get; }

    string? UserAgent { get; }
}