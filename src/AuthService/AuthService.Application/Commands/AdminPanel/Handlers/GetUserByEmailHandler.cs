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
///     Возвращает пользователя по e-mail.
/// </summary>
public sealed class GetUserByEmailHandler
    : ICommandHandler<UserAdminItemResponse, GetUserByEmailCommand>
{
    private readonly ILogger<GetUserByEmailHandler> _logger;
    private readonly UserManager<User> _userManager;

    public GetUserByEmailHandler(
        UserManager<User> userManager,
        ILogger<GetUserByEmailHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result<UserAdminItemResponse, ErrorList>> Handle(
        GetUserByEmailCommand command,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return Result.Failure<UserAdminItemResponse, ErrorList>(
                Errors.General.ValueIsRequired(nameof(command.Email)).ToErrorList());
        }

        string normalized = command.Email.Trim().ToUpperInvariant();

        IQueryable<User> query = _userManager.Users;
        if (command.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        User? user = await query.FirstOrDefaultAsync(
            u => u.NormalizedEmail == normalized, ct);

        if (user is null)
        {
            _logger.LogWarning("Пользователь не найден по email: {Email}", command.Email);
            return Result.Failure<UserAdminItemResponse, ErrorList>(
                Errors.General.NotFound(command.Email, "Пользователь").ToErrorList());
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
            user.LockoutEnd);

        _logger.LogInformation("Пользователь найден по email: {Email} -> {UserId}", command.Email, user.Id);
        return Result.Success<UserAdminItemResponse, ErrorList>(response);
    }
}