namespace AuthService.Contracts.Responses;

public sealed record ChangeEmailResponse(string Message, bool Success);

public sealed record ChangePasswordResponse(string Message, bool Success);

public sealed record DeleteProfileResponse(string Message);

public sealed record MyProfileResponse(string FullName, string Email, string Description);