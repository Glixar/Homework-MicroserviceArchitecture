using AuthService.Application.Abstractions;

namespace AuthService.Application.Commands.Accounts.Commands;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand;