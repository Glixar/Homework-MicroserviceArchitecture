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
///     Возвращает страницу пользователей с общим количеством.
/// </summary>
public sealed class GetAllUsersHandler
    : ICommandHandler<GetAllUsersResponse, GetAllUsersCommand>
{
    private readonly ILogger<GetAllUsersHandler> _logger;
    private readonly UserManager<User> _userManager;

    public GetAllUsersHandler(
        UserManager<User> userManager,
        ILogger<GetAllUsersHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result<GetAllUsersResponse, ErrorList>> Handle(
        GetAllUsersCommand command,
        CancellationToken ct)
    {
        if (command.Offset < 0)
        {
            return Result.Failure<GetAllUsersResponse, ErrorList>(
                Errors.General.ValueIsInvalid(nameof(command.Offset)).ToErrorList());
        }

        if (command.Limit <= 0)
        {
            return Result.Failure<GetAllUsersResponse, ErrorList>(
                Errors.General.ValueIsInvalid(nameof(command.Limit)).ToErrorList());
        }

        // Источник данных (включая/исключая soft-deleted)
        IQueryable<User> query = _userManager.Users;
        if (command.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        int total = await query.CountAsync(ct);
        List<User> users = await query
            .OrderBy(u => u.Email) // стабильная сортировка
            .Skip(command.Offset)
            .Take(command.Limit)
            .ToListAsync(ct);

        List<UserAdminItemResponse> items = users.Select(u =>
                new UserAdminItemResponse(
                    u.Id,
                    u.Email,
                    u.UserName!,
                    u.Description,
                    u.IsDeleted,
                    u.DeletedAtUtc,
                    u.EmailConfirmed,
                    u.LockoutEnabled,
                    u.LockoutEnd))
            .ToList();

        GetAllUsersResponse response = new(
            items,
            total,
            command.Offset,
            command.Limit);

        _logger.LogInformation(
            "Получен список пользователей: offset={Offset}, limit={Limit}, total={Total}, includeDeleted={IncludeDeleted}",
            command.Offset, command.Limit, total, command.IncludeDeleted);

        return Result.Success<GetAllUsersResponse, ErrorList>(response);
    }
}