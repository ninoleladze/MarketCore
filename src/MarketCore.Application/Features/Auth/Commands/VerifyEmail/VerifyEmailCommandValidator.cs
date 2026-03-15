using FluentValidation;

namespace MarketCore.Application.Features.Auth.Commands.VerifyEmail;

public sealed class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Verification token is required.")
            .Length(32).WithMessage("Invalid verification token format.");
    }
}
