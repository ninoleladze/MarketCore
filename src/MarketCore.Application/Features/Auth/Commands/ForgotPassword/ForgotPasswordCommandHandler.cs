using MediatR;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;
using Microsoft.Extensions.Logging;

namespace MarketCore.Application.Features.Auth.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IUnitOfWork uow,
        IEmailService emailService,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _uow = uow;
        _emailService = emailService;
        _logger = logger;
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

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await _emailService.SendPasswordResetAsync(
                user.Email.Value, user.FirstName, resetUrl, cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[FORGOT-PASSWORD] Failed to send reset email to {Email}: {Error}",
                user.Email.Value, ex.Message);
        }

        return Result.Success();
    }
}
