using AuthService.Application.JWT;
using AuthService.Domain;

namespace AuthService.Application.Abstractions;

public interface ITokenProvider
{
    Task<AccessTokenResult> GenerateAccessToken(User user, CancellationToken cancellationToken);

    Task<RefreshSession> GenerateRefreshToken(User user, Guid accessTokenJti, CancellationToken cancellationToken);
}