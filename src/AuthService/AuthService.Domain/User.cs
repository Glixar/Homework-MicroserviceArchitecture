using System.ComponentModel.DataAnnotations;

namespace AuthService.Domain;

/// <summary>
/// Простая доменная модель пользователя.
/// </summary>
public sealed class User
{
    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Логин / имя пользователя.
    /// </summary>
    [Required(ErrorMessage = "Логин обязателен.")]
    [MaxLength(50, ErrorMessage = "Логин не должен превышать 50 символов.")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Имя.
    /// </summary>
    [Required(ErrorMessage = "Имя обязательно.")]
    [MaxLength(100, ErrorMessage = "Имя не должно превышать 100 символов.")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Фамилия.
    /// </summary>
    [Required(ErrorMessage = "Фамилия обязательна.")]
    [MaxLength(100, ErrorMessage = "Фамилия не должна превышать 100 символов.")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Email.
    /// </summary>
    [Required(ErrorMessage = "Email обязателен.")]
    [EmailAddress(ErrorMessage = "Некорректный формат email.")]
    [MaxLength(200, ErrorMessage = "Email не должен превышать 200 символов.")]
    public string Email { get; set; } = string.Empty;
}