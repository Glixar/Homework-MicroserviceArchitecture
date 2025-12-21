using AuthService.Application.Abstractions;

namespace AuthService.Application.Commands.Auth.Commands;

public record LoginCommand(string Email, string Password) : ICommand;