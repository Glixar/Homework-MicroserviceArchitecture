using AuthService.Application.Abstractions;
using AuthService.Application.Commands.Accounts.Commands;
using AuthService.Contracts.Responses;
using AuthService.Domain;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace AuthService.Application.Commands.Accounts.Handlers;

public sealed class GetMyProfileHandler
    : ICommandHandler<MyProfileResponse, GetMyProfileCommand>
{
    private readonly ILogger<GetMyProfileHandler> _logger;
    private readonly UserManager<User> _userManager;
    private readonly ICurrentUser _current;

    public GetMyProfileHandler(
        UserManager<User> userManager,
        ICurrentUser current,
        ILogger<GetMyProfileHandler> logger)
    {
        _userManager = userManager;
        _current = current;
        _logger = logger;
    }

    public async Task<Result<MyProfileResponse, ErrorList>> Handle(
        GetMyProfileCommand command,
        CancellationToken ct)
    {
        _logger.LogInformation("Получен запрос на получение профиля текущего пользователя");

        if (!_current.IsAuthenticated || !_current.UserId.HasValue)
        {
            _logger.LogWarning("Не удалось извлечь Id пользователя из токена: токен недействителен");
            return Result.Failure<MyProfileResponse, ErrorList>(
                Errors.Tokens.InvalidToken().ToErrorList());
        }

        string userIdStr = _current.UserId.Value.ToString();
        _logger.LogInformation("Идентификатор пользователя извлечён из токена: {UserId}", userIdStr);

        User? user = await _userManager.FindByIdAsync(userIdStr);
        if (user is null)
        {
            _logger.LogWarning("Пользователь с Id {UserId} не найден", userIdStr);
            return Result.Failure<MyProfileResponse, ErrorList>(
                Errors.General.NotFound(userIdStr, "Пользователь"));
        }

        _logger.LogInformation("Пользователь найден. Формируем ответ профиля");

        MyProfileResponse response = new(
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.Description ?? string.Empty
        );

        _logger.LogInformation("Профиль пользователя {UserId} успешно получен", user.Id);
        return Result.Success<MyProfileResponse, ErrorList>(response);
    }
}