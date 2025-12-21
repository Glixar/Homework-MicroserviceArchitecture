using System.Text;
using AuthService.Contracts.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Contracts.Models;

public static class TokenValidationParametersFactory
{
    public static TokenValidationParameters CreateWithLifeTime(JwtOptions jwtOptions) =>
        new()
        {
            ValidIssuer = jwtOptions.Issuer, // Кто выпустил токен (Issuer)
            ValidAudience = jwtOptions.Audience, // Для кого токен (Audience)
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)), // Ключ для проверки подписи (Symmetric Key)
            ValidateIssuer = true, // Проверять ли Issuer
            ValidateAudience = true, // Проверять ли Audience
            ValidateLifetime = true, // Проверять ли срок действия токена
            ClockSkew = TimeSpan.Zero, // Нет временной поблажки для Expiration
            ValidateIssuerSigningKey = true, // Проверять ли подпись
            RoleClaimType = CustomClaims.Role // Указываем claim для ролей
        };

    public static TokenValidationParameters CreateWithoutLifeTime(JwtOptions jwtOptions) =>
        new()
        {
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false, // Срок действия игнорируется
            ValidateIssuerSigningKey = true,
            RoleClaimType = CustomClaims.Role
        };
}