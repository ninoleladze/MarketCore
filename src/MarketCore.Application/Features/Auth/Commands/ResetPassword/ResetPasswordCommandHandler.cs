using MediatR;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Auth.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordCommandHandler(IUnitOfWork uow, IPasswordHasher passwordHasher)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _uow.Users.GetByPasswordResetTokenAsync(request.Token, cancellationToken);

        if (user is null)
            return Result.Failure("Invalid or expired password reset link.");

        var newHash = _passwordHasher.Hash(request.NewPassword);
        var result = user.ResetPassword(request.Token, newHash);

        if (result.IsFailure)
            return result;

        await _uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
