using AuthService.Application.Abstractions;
using AuthService.Application.Commands.AdminPanel.Commands;
using AuthService.Contracts.Responses;
using AuthService.Domain;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace AuthService.Application.Commands.AdminPanel.Handlers;

/// <summary>
///     Возвращает пользователя по Id.
/// </summary>
public sealed class GetUserByIdHandler
    : ICommandHandler<UserAdminItemResponse, GetUserByIdCommand>
{
    private readonly ILogger<GetUserByIdHandler> _logger;
    private readonly UserManager<User> _userManager;

    public GetUserByIdHandler(
        UserManager<User> userManager,
        ILogger<GetUserByIdHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result<UserAdminItemResponse, ErrorList>> Handle(
        GetUserByIdCommand command,
        CancellationToken ct)
    {
        IQueryable<User> query = _userManager.Users;
        if (command.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        User? user = await query.FirstOrDefaultAsync(u => u.Id == command.Id, ct);
        if (user is null)
        {
            _logger.LogWarning("Пользователь не найден: {UserId}", command.Id);
            return Result.Failure<UserAdminItemResponse, ErrorList>(
                Errors.General.NotFound(command.Id.ToString(), "Пользователь").ToErrorList());
        }

        UserAdminItemResponse response = new(
            user.Id,
            user.Email,
            user.UserName!,
            user.Description,
            user.IsDeleted,
            user.DeletedAtUtc,
            user.EmailConfirmed,
            user.LockoutEnabled,
            user.LockoutEnd
        );

        _logger.LogInformation("Пользователь найден: {UserId}", user.Id);
        return Result.Success<UserAdminItemResponse, ErrorList>(response);
    }
}