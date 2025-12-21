using AuthService.Domain;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Infrastructure.Postgres.IdentityValidators;

/// <summary>
///     Кастомный валидатор пользователя, который убирает проверку на уникальность UserName,
///     но оставляет все остальные стандартные проверки (формат, уникальность e-mail и т.п.).
/// </summary>
internal sealed class UserValidatorAllowDuplicateUserName : UserValidator<User>
{
    public UserValidatorAllowDuplicateUserName(IdentityErrorDescriber? errors = null)
        : base(errors)
    {
    }

    public override async Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user)
    {
        IdentityResult baseResult = await base.ValidateAsync(manager, user);

        if (baseResult.Succeeded)
        {
            return baseResult;
        }

        IdentityError[] filteredErrors = baseResult.Errors
            .Where(e => !string.Equals(
                e.Code,
                nameof(IdentityErrorDescriber.DuplicateUserName),
                StringComparison.Ordinal))
            .ToArray();

        return filteredErrors.Length == 0
            ? IdentityResult.Success
            : IdentityResult.Failed(filteredErrors);
    }
}