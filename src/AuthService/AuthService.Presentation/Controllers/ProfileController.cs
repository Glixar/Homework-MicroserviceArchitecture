using AuthService.Application.Commands.Accounts.Commands;
using AuthService.Application.Commands.Accounts.Handlers;
using AuthService.Contracts.Requests;
using AuthService.Contracts.Responses;
using AuthService.Presentation.Permissions;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace AuthService.Presentation.Controllers;

[ApiController]
[Route("api/v1/account")]
public sealed class ProfileController : ControllerBase
{
    [Permission("users.service")]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(
        [FromServices] GetMyProfileHandler handler,
        [FromServices] ILogger<AdminPanelController> logger,
        CancellationToken ct)
    {
        GetMyProfileCommand command = new();
        Result<MyProfileResponse, ErrorList> result = await handler.Handle(command, ct);
        return result.IsFailure ? result.Error.ToResponse() : Ok(result.Value);
    }

    [Permission("users.service")]
    [HttpPatch("profile")]
    public async Task<IActionResult> PatchProfile(
        [FromBody] UpdateProfileRequest request,
        [FromServices] UpdateMyProfileHandler handler,
        [FromServices] ILogger<AdminPanelController> logger,
        CancellationToken ct)
    {
        UpdateMyProfileCommand command = new(request.FullName, request.Description);
        Result<MyProfileResponse, ErrorList> result = await handler.Handle(command, ct);
        return result.IsFailure ? result.Error.ToResponse() : Ok(result.Value);
    }

    [Permission("users.service")]
    [HttpPut("email")]
    public async Task<IActionResult> PutEmail(
        [FromBody] ChangeEmailRequest request,
        [FromServices] ChangeEmailHandler handler,
        [FromServices] ILogger<AdminPanelController> logger,
        CancellationToken ct)
    {
        ChangeEmailCommand command = new(request.NewEmail, request.Password);
        Result<ChangeEmailResponse, ErrorList> result = await handler.Handle(command, ct);
        return result.IsFailure ? result.Error.ToResponse() : Ok(result.Value);
    }

    [Permission("users.service")]
    [HttpPut("password")]
    public async Task<IActionResult> PutPassword(
        [FromBody] ChangePasswordRequest request,
        [FromServices] ChangePasswordHandler handler,
        [FromServices] ILogger<AdminPanelController> logger,
        CancellationToken ct)
    {
        ChangePasswordCommand command = new(request.OldPassword, request.NewPassword);
        Result<ChangePasswordResponse, ErrorList> result = await handler.Handle(command, ct);
        return result.IsFailure ? result.Error.ToResponse() : Ok(result.Value);
    }

    [Permission("users.service")]
    [HttpDelete("profile")]
    public async Task<IActionResult> DeleteProfile(
        [FromBody] DeleteProfileRequest request,
        [FromServices] DeleteMyProfileHandler handler,
        [FromServices] ILogger<AdminPanelController> logger,
        CancellationToken ct)
    {
        DeleteMyProfileCommand command = new(request.Password);
        Result<DeleteProfileResponse, ErrorList> result = await handler.Handle(command, ct);
        return result.IsFailure ? result.Error.ToResponse() : Ok(result.Value);
    }
}