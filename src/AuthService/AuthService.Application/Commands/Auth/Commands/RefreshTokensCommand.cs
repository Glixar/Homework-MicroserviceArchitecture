using AuthService.Application.Abstractions;

namespace AuthService.Application.Commands.Auth.Commands;

public record RefreshTokensCommand(Guid RefreshToken) : ICommand;