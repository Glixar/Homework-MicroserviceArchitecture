using AuthService.Application.Abstractions;

namespace AuthService.Application.Commands.Auth.Commands;
public record LogoutCommand(Guid RefreshToken, bool AllDevices) : ICommand;