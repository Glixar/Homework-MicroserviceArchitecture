namespace AuthService.Contracts.Requests;

/// <summary>Проверка e-mail.</summary>
public sealed record CheckEmailRequest(
    string Email);

/// <summary>Вход.</summary>
public sealed record LoginRequest(
    string Email,
    string Password);

/// <summary>Выход.</summary>
public sealed record LogoutRequest(
    Guid RefreshToken,
    bool AllDevices);

/// <summary>Обновление токенов.</summary>
public sealed record RefreshTokensRequest(Guid RefreshToken);

/// <summary>Регистрация.</summary>
public sealed record RegisterRequest(
    string Email,
    string Password,
    string FullName);