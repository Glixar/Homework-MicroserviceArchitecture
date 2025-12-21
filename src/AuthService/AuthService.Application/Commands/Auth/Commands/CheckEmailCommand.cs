using AuthService.Application.Abstractions;

namespace AuthService.Application.Commands.Auth.Commands;

public record CheckEmailCommand(string Email) : ICommand;