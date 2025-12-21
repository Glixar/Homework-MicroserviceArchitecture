namespace AuthService.Application.JWT;

public record AccessTokenResult(string AccessToken, Guid Jti, DateTime ValidFrom, DateTime ValidTo);