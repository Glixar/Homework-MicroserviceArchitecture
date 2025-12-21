using AuthService.Application.Abstractions;

namespace AuthService.Application.Commands.Accounts.Commands;

public sealed record UpdateMyProfileCommand(string? FullName, string? Description) : ICommand;