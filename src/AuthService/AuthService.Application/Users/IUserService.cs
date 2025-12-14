using AuthService.Contracts.Users;

namespace AuthService.Application.Users;

/// <summary>
/// Аппликационный сервис для работы с пользователями.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Получить всех пользователей.
    /// </summary>
    Task<IReadOnlyCollection<UserResponse>> GetAllAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Получить пользователя по идентификатору.
    /// </summary>
    Task<UserResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Создать нового пользователя.
    /// </summary>
    Task<UserResponse> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Обновить существующего пользователя.
    /// </summary>
    Task<UserResponse?> UpdateAsync(
        int id,
        UpdateUserRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Удалить пользователя.
    /// </summary>
    Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken);
}