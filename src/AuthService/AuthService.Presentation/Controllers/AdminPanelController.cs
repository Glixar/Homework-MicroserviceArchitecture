using AuthService.Application.Commands.AdminPanel.Commands;
using AuthService.Application.Commands.AdminPanel.Handlers;
using AuthService.Contracts.Requests;
using AuthService.Contracts.Responses;
using AuthService.Presentation.Permissions;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel;

namespace AuthService.Presentation.Controllers;

[ApiController]
[Route("api/v1/admin-panel")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Permission("system.admin")]
public sealed class AdminPanelController : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] GetAllUsersRequest req,
        [FromServices] GetAllUsersHandler handler,
        CancellationToken ct)
    {
        GetAllUsersCommand cmd = new(req.Offset, req.Limit, req.IncludeDeleted);
        Result<GetAllUsersResponse, ErrorList> result = await handler.Handle(cmd, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> GetUserById(
        [FromRoute] GetUserByIdRequest req,
        [FromServices] GetUserByIdHandler handler,
        CancellationToken ct)
    {
        GetUserByIdCommand cmd = new(req.Id, req.IncludeDeleted);
        Result<UserAdminItemResponse, ErrorList> result = await handler.Handle(cmd, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("users/by-email")]
    public async Task<IActionResult> GetUserByEmail(
        [FromBody] GetUserByEmailRequest req,
        [FromServices] GetUserByEmailHandler handler,
        CancellationToken ct)
    {
        GetUserByEmailCommand cmd = new(req.Email, req.IncludeDeleted);
        Result<UserAdminItemResponse, ErrorList> result = await handler.Handle(cmd, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeleteUserById(
        [FromRoute] DeleteUserByIdRequest req,
        [FromServices] DeleteUserByIdHandler handler,
        CancellationToken ct)
    {
        DeleteUserByIdCommand cmd = new(req.Id);
        Result<DeleteUserResponse, ErrorList> result = await handler.Handle(cmd, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("users/restore")]
    public async Task<IActionResult> RestoreUser(
        [FromBody] RestoreUserRequest req,
        [FromServices] RestoreUserHandler handler,
        CancellationToken ct)
    {
        RestoreUserCommand cmd = new(req.Id);
        Result<RestoreUserResponse, ErrorList> result = await handler.Handle(cmd, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest req,
        [FromServices] CreateUserHandler handler,
        CancellationToken ct)
    {
        CreateUserCommand cmd = new(req.Email, req.Password, req.FullName, req.Roles, req.Permissions, req.Description);
        Result<CreateUserResponse, ErrorList> result = await handler.Handle(cmd, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPatch("users")]
    public async Task<IActionResult> UpdateUser(
        [FromBody] UpdateUserRequest req,
        [FromServices] UpdateUserHandler handler,
        CancellationToken ct)
    {
        UpdateUserCommand cmd = new(req.Id, req.Email, req.FullName, req.Description, req.Lockout, req.Roles, req.Permissions);

        Result<UpdateUserResponse, ErrorList> result = await handler.Handle(cmd, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}