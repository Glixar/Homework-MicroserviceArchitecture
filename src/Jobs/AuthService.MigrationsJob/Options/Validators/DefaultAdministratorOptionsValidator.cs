using Microsoft.Extensions.Options;

namespace AuthService.MigrationsJob.Options.Validators
{
    public sealed class DefaultAdministratorOptionsValidator : IValidateOptions<DefaultAdministratorOptions>
    {
        public ValidateOptionsResult Validate(string? name, DefaultAdministratorOptions options)
        {
            if (options.Apply)
            {
                if (string.IsNullOrWhiteSpace(options.UserName))
                {
                    return ValidateOptionsResult.Fail("DefaultAdministrator.UserName обязателен.");
                }

                if (string.IsNullOrWhiteSpace(options.Email))
                {
                    return ValidateOptionsResult.Fail("DefaultAdministrator.Email обязателен.");
                }

                if (string.IsNullOrWhiteSpace(options.Password))
                {
                    return ValidateOptionsResult.Fail("DefaultAdministrator.Password обязателен и не может быть пустым.");
                }

                if (options.Password.Trim() == "__ENV__")
                {
                    return ValidateOptionsResult.Fail("DefaultAdministrator.Password должен приходить из ENV; плейсхолдер '__ENV__' в файле конфигурации запрещён.");
                }
            }

            return ValidateOptionsResult.Success;
        }
    }
}