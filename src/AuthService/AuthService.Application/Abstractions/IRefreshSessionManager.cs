using AuthService.Domain;
using CSharpFunctionalExtensions;
using SharedKernel;

namespace AuthService.Application.Abstractions;

public interface IRefreshSessionManager
{
    Task<Result<RefreshSession, Error>> GetByRefreshToken(string refreshToken, CancellationToken ct);

    Task<UnitResult<Error>> Delete(RefreshSession session, CancellationToken ct);

    Task<UnitResult<Error>> DeleteAllByUserId(Guid userId, CancellationToken ct);
}