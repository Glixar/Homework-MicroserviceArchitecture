using AuthService.Application.Abstractions;
using AuthService.Application.Commands.Auth.Commands;
using AuthService.Application.JWT;
using AuthService.Contracts.Responses;
using AuthService.Domain;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace AuthService.Application.Commands.Auth.Handlers;

public sealed class RegisterHandler : ICommandHandler<TokensResponse, RegisterCommand>
{
    private readonly ILogger<RegisterHandler> _logger;
    private readonly ITokenProvider _tokenProvider;
    private readonly UserManager<User> _userManager;

    public RegisterHandler(
        UserManager<User> userManager,
        ITokenProvider tokenProvider,
        ILogger<RegisterHandler> logger)
    {
        _userManager = userManager;
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    public async Task<Result<TokensResponse, ErrorList>> Handle(RegisterCommand command, CancellationToken ct)
    {
        // Валидация ввода
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return Result.Failure<TokensResponse, ErrorList>(
                Errors.General.ValueIsRequired("Email").ToErrorList());
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return Result.Failure<TokensResponse, ErrorList>(
                Errors.General.ValueIsRequired("Password").ToErrorList());
        }

        // Проверяем существование пользователя с игнорированием soft-delete
        string normalizedEmail = _userManager.NormalizeEmail(command.Email);

        User? existingAny = await _userManager.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);

        if (existingAny is not null)
        {
            // Если когда-то уже регистрировались на этот e-mail — запрещаем повторную регистрацию
            if (existingAny.IsDeleted)
            {
                _logger.LogWarning(
                    "Регистрация отклонена: e-mail {Email} принадлежит удалённому аккаунту (Id={UserId}).",
                    command.Email, existingAny.Id);
                return Result.Failure<TokensResponse, ErrorList>(
                    Errors.General.Conflict("Аккаунт с таким e-mail ранее был удалён. Повторная регистрация запрещена.")
                        .ToErrorList());
            }

            _logger.LogWarning("Регистрация отклонена: e-mail {Email} уже занят (Id={UserId})", command.Email,
                existingAny.Id);
            return Result.Failure<TokensResponse, ErrorList>(Errors.General.AlreadyExist("E-mail").ToErrorList());
        }

        // Создание пользователя
        User user = User.CreateUser(command.FullName, command.Email, string.Empty);

        IdentityResult createRes = await _userManager.CreateAsync(user, command.Password);
        if (!createRes.Succeeded)
        {
            _logger.LogError("Не удалось создать пользователя {Email}: {Errors}",
                command.Email, string.Join(", ", createRes.Errors.Select(e => $"{e.Code}:{e.Description}")));
            return Result.Failure<TokensResponse, ErrorList>(Errors.General.Failure().ToErrorList());
        }

        // Роль по умолчанию
        await _userManager.AddToRoleAsync(user, "USER");

        // Выпуск токенов
        AccessTokenResult accessToken = await _tokenProvider.GenerateAccessToken(user, ct);
        RefreshSession refreshToken = await _tokenProvider.GenerateRefreshToken(user, accessToken.Jti, ct);

        _logger.LogInformation("Регистрация завершена: пользователь {UserId} создан", user.Id);

        return new TokensResponse(accessToken.AccessToken, accessToken.ValidTo, refreshToken.RefreshToken,
            refreshToken.ExpiresIn);
    }
}