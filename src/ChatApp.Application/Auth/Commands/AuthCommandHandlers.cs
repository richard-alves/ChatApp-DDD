using ChatApp.Application.Common;
using ChatApp.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ChatApp.Application.Auth.Commands;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class RegisterCommandHandler(IAuthService authService) : IRequestHandler<RegisterCommand, Result<AuthResponseDto>>
{
    public Task<Result<AuthResponseDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
        => authService.RegisterAsync(request.Username, request.Email, request.Password, cancellationToken);
}

public class LoginCommandHandler(IAuthService authService) : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    public Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        => authService.LoginAsync(request.Email, request.Password, cancellationToken);
}
