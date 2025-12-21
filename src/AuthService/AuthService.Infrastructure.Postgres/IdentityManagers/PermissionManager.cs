using AuthService.Application.Abstractions;
using AuthService.Contracts.Models;
using AuthService.Domain;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace AuthService.Infrastructure.Postgres.IdentityManagers;

public sealed class PermissionManager : IPermissionManager
{
    private readonly PostgresDbContext _dbContext;
    private readonly ILogger<PermissionManager> _logger;
    private readonly UserManager<User> _userManager;

    public PermissionManager(
        PostgresDbContext dbContext,
        UserManager<User> userManager,
        ILogger<PermissionManager> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    ///     Найти пермишен по коду.
    /// </summary>
    public async Task<Permission?> FindByCodeAsync(string code, CancellationToken ct)
        => await _dbContext.Permissions
            .FirstOrDefaultAsync(p => p.Code == code, ct);

    public Task AddRangeIfNotExistsAsync(IEnumerable<string> permissionCodes, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<HashSet<string>> GetUserPermissionCodesAsync(Guid userId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    /// <summary>
    ///     Полностью заменить пользовательские пермишены (клеймы) на заданный набор существующих кодов.
    /// </summary>
    public async Task<UnitResult<Error>> ReplaceUserPermissionsAsync(
        Guid userId,
        IEnumerable<string> permissionCodes,
        CancellationToken ct)
    {
        User? user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return UnitResult.Failure(Errors.General.NotFound(userId.ToString(), "Пользователь"));
        }

        // Нормализуем вход
        string[] requested = permissionCodes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // Оставляем только существующие в справочнике
        List<string> allowed = await _dbContext.Permissions
            .Where(p => requested.Contains(p.Code))
            .Select(p => p.Code)
            .ToListAsync(ct);

        // Текущие пользовательские клеймы пермишенов
        List<IdentityUserClaim<Guid>> userClaims = await _dbContext.UserClaims
            .Where(c => c.UserId == user.Id && c.ClaimType == CustomClaims.Permission)
            .ToListAsync(ct);

        HashSet<string> current = userClaims.Select(c => c.ClaimValue!).ToHashSet(StringComparer.Ordinal);

        List<IdentityUserClaim<Guid>> toRemove = userClaims.Where(c => !allowed.Contains(c.ClaimValue!)).ToList();
        string[] toAdd = allowed.Where(code => !current.Contains(code)).ToArray();

        if (toRemove.Count > 0)
        {
            _dbContext.UserClaims.RemoveRange(toRemove);
        }

        if (toAdd.Length > 0)
        {
            IEnumerable<IdentityUserClaim<Guid>> newClaims = toAdd.Select(code => new IdentityUserClaim<Guid>
            {
                UserId = user.Id, ClaimType = CustomClaims.Permission, ClaimValue = code
            });

            await _dbContext.UserClaims.AddRangeAsync(newClaims, ct);
        }

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Пермишены пользователя {UserId} обновлены. Удалено: {Removed}, добавлено: {Added}.",
            user.Id, toRemove.Count, toAdd.Length);

        return UnitResult.Success<Error>();
    }

    /// <summary>
    ///     Назначить пользователю пермишен (клейм) по коду, если он существует в справочнике.
    /// </summary>
    public async Task<UnitResult<Error>> AssignPermissionToUserIfExistsAsync(
        Guid userId,
        string permissionCode,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return UnitResult.Failure(Errors.General.ValueIsRequired("Permission"));
        }

        string code = permissionCode.Trim();

        bool exists = await _dbContext.Permissions.AnyAsync(p => p.Code == code, ct);
        if (!exists)
        {
            return UnitResult.Failure(Errors.General.NotFound(name: "Пермишен"));
        }

        User? user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return UnitResult.Failure(Errors.General.NotFound(userId.ToString(), "Пользователь"));
        }

        bool already = await _dbContext.UserClaims
            .AnyAsync(c => c.UserId == user.Id && c.ClaimType == CustomClaims.Permission && c.ClaimValue == code, ct);

        if (already)
        {
            return UnitResult.Success<Error>();
        }

        await _dbContext.UserClaims.AddAsync(
            new IdentityUserClaim<Guid> { UserId = user.Id, ClaimType = CustomClaims.Permission, ClaimValue = code },
            ct);

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Пользователю {UserId} назначен пермишен {Code}.", user.Id, code);
        return UnitResult.Success<Error>();
    }

    /// <summary>
    ///     Массово добавить пермишены, если они отсутствуют в справочнике.
    /// </summary>
    public async Task AddRangeIfNotExists(IEnumerable<string> permissionCodes, CancellationToken ct)
    {
        string[] codes = permissionCodes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (codes.Length == 0)
        {
            return;
        }

        List<string> existed = await _dbContext.Permissions
            .Where(p => codes.Contains(p.Code))
            .Select(p => p.Code)
            .ToListAsync(ct);

        string[] missing = codes.Except(existed, StringComparer.Ordinal).ToArray();
        if (missing.Length == 0)
        {
            return;
        }

        await _dbContext.Permissions.AddRangeAsync(
            missing.Select(code => new Permission { Code = code }), ct);

        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Добавлено пермишенов в справочник: {Count}.", missing.Length);
    }

    /// <summary>
    ///     Получить набор кодов пермишенов пользователя (из ролей + пользовательских клеймов).
    /// </summary>
    public async Task<HashSet<string>> GetUserPermissionCodes(Guid userId, CancellationToken ct)
    {
        // Из ролей
        List<string> roleCodes = await _dbContext.Users
            .Where(u => u.Id == userId)
            .SelectMany(u => u.Roles)
            .SelectMany(r => r.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .ToListAsync(ct);

        // Из клеймов пользователя
        List<string> claimCodes = await _dbContext.UserClaims
            .Where(c => c.UserId == userId && c.ClaimType == CustomClaims.Permission)
            .Select(c => c.ClaimValue!)
            .ToListAsync(ct);

        return roleCodes
            .Concat(claimCodes)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToHashSet(StringComparer.Ordinal);
    }
}