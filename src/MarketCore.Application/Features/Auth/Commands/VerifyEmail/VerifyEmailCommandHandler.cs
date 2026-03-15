using MediatR;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Auth.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result>
{
    private readonly IUnitOfWork _uow;

    public VerifyEmailCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _uow.Users.GetByVerificationTokenAsync(request.Token, cancellationToken);

        if (user is null)
            return Result.Failure("Invalid or expired verification link.");

        var result = user.ConfirmEmail(request.Token);
        if (result.IsFailure)
            return result;

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
