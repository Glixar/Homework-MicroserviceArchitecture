namespace SharedKernel;

public static class Errors
{
    public static class General
    {
        private const string ValueRequiredCode = "core.value.validate.required";

        public static Error Failure() =>
            Error.Failure(
                "core.operation.execute.failure",
                "Произошла ошибка");

        public static Error Conflict(string message, string? code = null) =>
            Error.Conflict(
                code ?? "core.operation.conflict",
                message);

        public static Error Forbidden(string? reason = null)
        {
            string suffix = string.IsNullOrWhiteSpace(reason) ? string.Empty : $": {reason}";
            return Error.Failure(
                "core.auth.forbidden",
                $"Доступ запрещён{suffix}");
        }

        public static Error ValueIsInvalid(string? name = null)
        {
            string label = name ?? "Значение";
            return Error.Validation(
                "core.value.validate.invalid",
                $"{label} недопустимо",
                name);
        }

        public static Error NotFound(string id = null, string? name = null)
        {
            string label = name ?? "Запись";
            string forId = id == null ? string.Empty : $" (Id: {id})";
            return Error.NotFound(
                "core.record.get.not-found",
                $"{label} не найдена{forId}");
        }

        public static Error ValueIsRequired(string? name = null)
        {
            string label = name ?? "Значение";
            return Error.Validation(
                ValueRequiredCode,
                $"{label} является обязательным",
                name);
        }

        public static Error AlreadyExist(string? name = null)
        {
            string label = name ?? "Запись";
            return Error.Conflict(
                "core.record.create.already-exists",
                $"{label} уже существует");
        }

        public static Error Failure(string cannotCreateUser) =>
            Error.Failure(
                "core.operation.execute.failure",
                string.IsNullOrWhiteSpace(cannotCreateUser)
                    ? "Произошла ошибка"
                    : cannotCreateUser);

        public static Error NotAllowed(string? action = null)
        {
            string suffix = action is null ? string.Empty : $": {action}";
            return Error.Failure(
                "core.operation.perform.not-allowed",
                $"Действие запрещено{suffix}");
        }

        public static Error Unexpected(string? message = null) =>
            Error.Failure(
                "core.operation.execute.unexpected",
                string.IsNullOrWhiteSpace(message)
                    ? "Произошла непредвиденная ошибка"
                    : message);
    }

    public static class User
    {
        public static Error InvalidCredentials() =>
            Error.Validation(
                "core.auth.user.validate.credentials-invalid",
                "Неверные учётные данные");
    }

    public static class Tokens
    {
        public static Error ExpiredToken() =>
            Error.Validation(
                "core.auth.token.refresh.expired",
                "Срок действия токена истёк");

        public static Error InvalidToken() =>
            Error.Validation(
                "core.auth.token.validate.invalid",
                "Токен недействителен");
    }
}