using AuthService.Domain;
using CSharpFunctionalExtensions;
using SharedKernel;

namespace AuthService.Application.Abstractions;

public interface IPermissionManager
{
    Task<Permission?> FindByCodeAsync(
        string code,
        CancellationToken cancellationToken);

    Task AddRangeIfNotExistsAsync(
        IEnumerable<string> permissionCodes,
        CancellationToken cancellationToken);

    Task<HashSet<string>> GetUserPermissionCodesAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<UnitResult<Error>> ReplaceUserPermissionsAsync(
        Guid userId,
        IEnumerable<string> permissionCodes,
        CancellationToken cancellationToken);

    Task<UnitResult<Error>> AssignPermissionToUserIfExistsAsync(
        Guid userId,
        string permissionCode,
        CancellationToken cancellationToken);
}