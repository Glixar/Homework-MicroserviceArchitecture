using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Application.Abstractions;
using AuthService.Application.JWT;
using AuthService.Contracts.Models;
using AuthService.Contracts.Options;
using AuthService.Domain;
using AuthService.Infrastructure.Postgres.IdentityManagers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Postgres;

public class JwtTokenProvider : ITokenProvider
{
    private readonly PostgresDbContext _accountWriteContext;
    private readonly JwtOptions _jwtOptions;
    private readonly PermissionManager _permissionManager;
    private readonly UserManager<User> _userManager;

    public JwtTokenProvider(
        IOptions<JwtOptions> options,
        PermissionManager permissionManager,
        PostgresDbContext accountWriteContext,
        UserManager<User> userManager)
    {
        _permissionManager = permissionManager ?? throw new ArgumentNullException(nameof(permissionManager));
        _accountWriteContext = accountWriteContext ?? throw new ArgumentNullException(nameof(accountWriteContext));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _jwtOptions = (options ?? throw new ArgumentNullException(nameof(options))).Value;
    }

    public async Task<AccessTokenResult> GenerateAccessToken(User user, CancellationToken cancellationToken)
    {
        SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        SigningCredentials signingCredentials = new(securityKey, SecurityAlgorithms.HmacSha256);

        IList<string> roleNames = await _userManager.GetRolesAsync(user);
        IEnumerable<Claim> roleClaims = roleNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(n => new Claim(CustomClaims.Role, n));

        HashSet<string> permissions = await _permissionManager.GetUserPermissionCodes(user.Id, cancellationToken);
        IEnumerable<Claim> permissionClaims = permissions
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(p => new Claim(CustomClaims.Permission, p));

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid jti = Guid.NewGuid();

        List<Claim> claims = new()
        {
            new Claim(CustomClaims.Id, user.Id.ToString()),
            new Claim(CustomClaims.UserName, user.UserName ?? string.Empty),
            new Claim(CustomClaims.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, jti.ToString()),
            new Claim(
                JwtRegisteredClaimNames.Iat,
                now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64),
        };

        claims.AddRange(roleClaims);
        claims.AddRange(permissionClaims);

        JwtSecurityToken jwtToken = new(
            _jwtOptions.Issuer,
            _jwtOptions.Audience,
            claims,
            now.UtcDateTime,
            now.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes).UtcDateTime,
            signingCredentials);

        string? jwtStringToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);

        return new AccessTokenResult(jwtStringToken, jti, jwtToken.ValidFrom, jwtToken.ValidTo);
    }

    public async Task<RefreshSession> GenerateRefreshToken(User user, Guid accessTokenJti,
        CancellationToken cancellationToken)
    {
        RefreshSession refreshSession = new()
        {
            Id = Guid.NewGuid(),
            User = user,
            ExpiresIn = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays),
            CreatedAt = DateTime.UtcNow,
            Jti = accessTokenJti,
            RefreshToken = Guid.NewGuid()
        };

        _accountWriteContext.Add(refreshSession);
        await _accountWriteContext.SaveChangesAsync(cancellationToken);

        return refreshSession;
    }
}