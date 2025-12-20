using AuthService.Application.Users;
using AuthService.Contracts.Users;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Presentation.Controllers;

/// <summary>
/// REST-контроллер для CRUD-операций по пользователям.
/// </summary>
[ApiController]
[Route("users")]
public sealed class UserController : ControllerBase
{
    private readonly IUserService _userService;

    /// <summary>
    /// Конструктор контроллера пользователей.
    /// </summary>
    /// <param name="userService">Сервис прикладного слоя для работы с пользователями.</param>
    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Получить список всех пользователей.
    /// </summary>
    /// <remarks>
    /// Возвращает полный список пользователей.
    /// </remarks>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>200 OK — список пользователей.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<UserResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllAsync(cancellationToken);
        return Ok(users);
    }

    /// <summary>
    /// Получить пользователя по идентификатору.
    /// </summary>
    /// <remarks>
    /// Если пользователь с указанным идентификатором не найден, возвращает 404 Not Found.
    /// </remarks>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>
    /// 200 OK — данные пользователя.  
    /// 404 Not Found — если пользователь не найден.
    /// </returns>
    [HttpGet("{user_id:int}", Name = "GetUserById")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(
        [FromRoute(Name = "user_id")] int userId,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>
    /// Создать пользователя.
    /// </summary>
    /// <remarks>
    /// Создаёт нового пользователя и возвращает 201 Created с телом созданного пользователя.
    /// </remarks>
    /// <param name="request">Запрос на создание пользователя.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>201 Created — созданный пользователь.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponse>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _userService.CreateAsync(request, cancellationToken);

        return CreatedAtRoute(
            routeName: "GetUserById",
            routeValues: new { user_id = created.Id },
            value: created);
    }

    /// <summary>
    /// Обновить пользователя.
    /// </summary>
    /// <remarks>
    /// Обновляет существующего пользователя. Если пользователь не найден — возвращает 404.
    /// </remarks>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="request">Запрос на обновление пользователя.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>
    /// 200 OK — обновлённый пользователь.  
    /// 404 Not Found — если пользователь не найден.
    /// </returns>
    [HttpPut("{user_id:int}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponse>> Update(
        [FromRoute(Name = "user_id")] int userId,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _userService.UpdateAsync(userId, request, cancellationToken);

        if (updated is null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    /// <summary>
    /// Удалить пользователя.
    /// </summary>
    /// <remarks>
    /// Удаляет пользователя. Если пользователь не найден — возвращает 404.
    /// </remarks>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>
    /// 204 No Content — если пользователь удалён.  
    /// 404 Not Found — если пользователь не найден.
    /// </returns>
    [HttpDelete("{user_id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute(Name = "user_id")] int userId,
        CancellationToken cancellationToken)
    {
        var deleted = await _userService.DeleteAsync(userId, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}