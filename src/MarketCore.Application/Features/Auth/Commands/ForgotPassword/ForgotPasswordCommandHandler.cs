using MediatR;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _emailService;

    public ForgotPasswordCommandHandler(IUnitOfWork uow, IEmailService emailService)
    {
        _uow = uow;
        _emailService = emailService;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _uow.Users.GetByEmailAsync(request.Email, cancellationToken);

        // Always return success to prevent user enumeration
        if (user is null)
            return Result.Success();

        var token = Guid.NewGuid().ToString("N");
        user.SetPasswordResetToken(token);
        await _uow.SaveChangesAsync(cancellationToken);

        var resetUrl = $"{request.ClientBaseUrl?.TrimEnd('/')}/auth/reset-password?token={token}";

        _ = _emailService.SendPasswordResetAsync(
            user.Email.Value, user.FirstName, resetUrl, CancellationToken.None);

        return Result.Success();
    }
}
