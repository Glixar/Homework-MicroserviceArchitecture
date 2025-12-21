using AuthService.Application.Abstractions;
using AuthService.Application.Commands.Auth.Commands;
using AuthService.Contracts.Responses;
using AuthService.Domain;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace AuthService.Application.Commands.Auth.Handlers;

public sealed class CheckEmailHandler : ICommandHandler<CheckEmailResponse, CheckEmailCommand>
{
    private readonly ILogger<CheckEmailHandler> _logger;
    private readonly UserManager<User> _userManager;

    public CheckEmailHandler(UserManager<User> userManager, ILogger<CheckEmailHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result<CheckEmailResponse, ErrorList>> Handle(CheckEmailCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return Result.Failure<CheckEmailResponse, ErrorList>(Errors.General.ValueIsInvalid(nameof(command.Email))
                .ToErrorList());
        }

        User? user = await _userManager.FindByEmailAsync(command.Email);
        bool exists = user is not null;
        _logger.LogInformation("CheckEmail: {Email} exists={Exists}", command.Email, exists);

        return new CheckEmailResponse(exists);
    }
}