using AuthService.Application.Abstractions;
using AuthService.Domain;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace AuthService.Infrastructure.Postgres.IdentityManagers;

public sealed class RefreshSessionManager : IRefreshSessionManager
{
    private readonly PostgresDbContext _db;

    public RefreshSessionManager(PostgresDbContext db) => _db = db;

    public async Task<Result<RefreshSession, Error>> GetByRefreshToken(string refreshToken, CancellationToken ct)
    {
        if (!Guid.TryParse(refreshToken, out Guid tokenGuid))
        {
            return Errors.General.ValueIsInvalid(nameof(refreshToken));
        }

        RefreshSession? session = await _db.RefreshSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.RefreshToken == tokenGuid, ct)
            .ConfigureAwait(false);

        if (session is null)
        {
            return Errors.General.ValueIsInvalid(nameof(refreshToken));
        }

        if (session.ExpiresIn <= DateTime.UtcNow)
        {
            return Errors.General.Forbidden("Refresh token expired");
        }

        return Result.Success<RefreshSession, Error>(session);
    }

    public async Task<UnitResult<Error>> DeleteAllByUserId(Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
        {
            return Errors.General.ValueIsInvalid(nameof(userId));
        }

        _ = await _db.RefreshSessions
            .Where(r => r.UserId == userId)
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);

        return UnitResult.Success<Error>();
    }

    public async Task<UnitResult<Error>> Delete(RefreshSession refreshSession, CancellationToken ct)
    {
        if (refreshSession is null)
        {
            return Errors.General.ValueIsInvalid(nameof(refreshSession));
        }

        _ = await _db.RefreshSessions
            .Where(r => r.Id == refreshSession.Id)
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);

        return UnitResult.Success<Error>();
    }
}