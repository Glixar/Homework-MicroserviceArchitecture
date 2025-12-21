namespace AuthService.Contracts.Responses;

public sealed record LogoutResponse(
    bool Success,
    string Scope,
    DateTimeOffset LoggedOutAt
);

public sealed record CheckEmailResponse(bool Exists);

public sealed record TokensResponse(
    string AccessToken,
    DateTimeOffset AccessExpiresAt,
    Guid RefreshToken,
    DateTimeOffset RefreshExpiresAt
);