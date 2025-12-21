using AuthService.Application.Commands.Auth.Commands;
using AuthService.Application.Commands.Auth.Handlers;
using AuthService.Contracts.Requests;
using AuthService.Contracts.Responses;
using AuthService.Presentation.Permissions;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace AuthService.Presentation.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    /// <summary>Логин. Возвращает пару токенов (как и register).</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] LoginHandler handler,
        [FromServices] ILogger<AuthController> logger,
        CancellationToken ct)
    {
        LoginCommand command = new(request.Email, request.Password);
        Result<TokensResponse, ErrorList> result = await handler.Handle(command, ct);
        return result.IsFailure ? result.Error.ToResponse() : Ok(result.Value);
    }

    /// <summary>Обновление access/refresh токенов.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshTokens(
        [FromBody] RefreshTokensRequest request,
        [FromServices] RefreshTokensHandler handler,
        [FromServices] ILogger<AuthController> logger,
        CancellationToken ct)
    {
        RefreshTokensCommand command = new(request.RefreshToken);
        Result<TokensResponse, ErrorList> result = await handler.Handle(command, ct);
        return result.IsFailure ? result.Error.ToResponse() : Ok(result.Value);
    }

    /// <summary>
    ///     Логаут. Инвалидирует текущую сессию (или все, если allDevices = true).
    /// </summary>
    [Permission("auth.service")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        [FromServices] LogoutHandler handler,
        [FromServices] ILogger<AuthController> logger,
        CancellationToken ct)
    {
        LogoutCommand command = new(request.RefreshToken, request.AllDevices);
        Result<LogoutResponse, ErrorList> result = await handler.Handle(command, ct);
        return result.IsFailure ? result.Error.ToResponse() : Ok(result.Value);
    }

    /// <summary>
    ///     Регистрация пользователя. Возвращает пару токенов (как и login).
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        [FromServices] RegisterHandler handler,
        [FromServices] ILogger<AuthController> logger,
        CancellationToken ct)
    {
        RegisterCommand command = new(request.Email, request.Password, request.FullName);
        Result<TokensResponse, ErrorList> result = await handler.Handle(command, ct);
        return result.IsFailure ? result.Error.ToResponse() : Ok(result.Value);
    }

    /// <summary>Проверка существования пользователя по email.</summary>
    [HttpPost("check-email")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckEmail(
        [FromBody] CheckEmailRequest request,
        [FromServices] CheckEmailHandler handler,
        [FromServices] ILogger<AuthController> logger,
        CancellationToken ct)
    {
        CheckEmailCommand command = new(request.Email);
        Result<CheckEmailResponse, ErrorList> result = await handler.Handle(command, ct);
        return result.IsFailure ? result.Error.ToResponse() : Ok(result.Value);
    }
}