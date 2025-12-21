using AuthService.Application.Abstractions;

namespace AuthService.Application.Commands.Auth.Commands;

public record RegisterCommand(string Email, string Password, string FullName) : ICommand;