using AuthService.Application.Abstractions;

namespace AuthService.Application.Commands.Accounts.Commands;

public sealed record ChangeEmailCommand(string NewEmail, string Password) : ICommand;