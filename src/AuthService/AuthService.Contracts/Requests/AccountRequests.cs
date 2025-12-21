namespace AuthService.Contracts.Requests;

/// <summary>Тело запроса для смены e-mail.</summary>
public sealed record ChangeEmailRequest(string NewEmail, string Password);

/// <summary>Тело запроса для смены пароля.</summary>
public sealed record ChangePasswordRequest(string OldPassword, string NewPassword);

/// <summary>Тело запроса для удаления аккаунта.</summary>
public sealed record DeleteProfileRequest(string Password);

/// <summary>Тело запроса для обновления профиля.</summary>
public sealed record UpdateProfileRequest(string? FullName, string? Description);