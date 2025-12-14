using AuthService.Application.Users;
using AuthService.Contracts.Users;
using AuthService.Domain;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Postgres.Users;

/// <summary>
/// Реализация IUserService поверх PostgresDbContext.
/// </summary>
public sealed class PostgresUserService : IUserService
{
    private readonly PostgresDbContext _dbContext;

    /// <summary>
    /// Конструктор сервиса пользователей.
    /// </summary>
    public PostgresUserService(PostgresDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<UserResponse>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .Select(user => new UserResponse
            {
                Id = user.Id,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email
            })
            .ToListAsync(cancellationToken);

        return users;
    }

    public async Task<UserResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (user is null)
        {
            return null;
        }

        return new UserResponse
        {
            Id = user.Id,
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email
        };
    }

    public async Task<UserResponse> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = new User
        {
            UserName = request.UserName,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UserResponse
        {
            Id = user.Id,
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email
        };
    }

    public async Task<UserResponse?> UpdateAsync(
        int id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (user is null)
        {
            return null;
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UserResponse
        {
            Id = user.Id,
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email
        };
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (user is null)
        {
            return false;
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}