using System.ComponentModel.DataAnnotations;

namespace AuthService.Contracts.Users;

/// <summary>
/// Запрос на обновление пользователя.
/// </summary>
public sealed class UpdateUserRequest
{
    // Имя
    [Required(ErrorMessage = "Имя обязательно.")]
    [MaxLength(100, ErrorMessage = "Максимальная длина имени — 100 символов.")]
    public string FirstName { get; init; } = default!;

    // Фамилия
    [Required(ErrorMessage = "Фамилия обязательна.")]
    [MaxLength(100, ErrorMessage = "Максимальная длина фамилии — 100 символов.")]
    public string LastName { get; init; } = default!;

    // Почта
    [Required(ErrorMessage = "E-mail обязателен.")]
    [EmailAddress(ErrorMessage = "Некорректный формат E-mail.")]
    [MaxLength(255, ErrorMessage = "Максимальная длина E-mail — 255 символов.")]
    public string Email { get; init; } = default!;
}