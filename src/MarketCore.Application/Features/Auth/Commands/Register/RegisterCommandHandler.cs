using MediatR;
using MarketCore.Application.Exceptions;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;
using MarketCore.Domain.Entities;
using MarketCore.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using CartEntity = MarketCore.Domain.Entities.Cart;

namespace MarketCore.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IUnitOfWork uow,
        IPasswordHasher passwordHasher,
        IEmailService emailService,
        ILogger<RegisterCommandHandler> logger)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var existingUser = await _uow.Users.GetByEmailAsync(request.Email, cancellationToken);

        if (existingUser is not null)
        {
            if (existingUser.IsEmailVerified)
                return Result.Failure($"An account with email '{request.Email}' already exists.");

            // Unverified — refresh token and resend code
            var freshToken = Random.Shared.Next(100_000, 999_999).ToString();
            existingUser.SetVerificationToken(freshToken);
            await _uow.SaveChangesAsync(cancellationToken);
            await TrySendAsync(existingUser.Email.Value, existingUser.FirstName, freshToken);
            return Result.Success();
        }

        Email emailVo;
        try
        {
            emailVo = new Email(request.Email);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = User.Create(emailVo, passwordHash, request.FirstName, request.LastName);

        var verificationToken = Random.Shared.Next(100_000, 999_999).ToString();
        user.SetVerificationToken(verificationToken);

        await _uow.Users.AddAsync(user, cancellationToken);

        var cart = CartEntity.Create(user.Id);
        await _uow.Carts.AddAsync(cart, cancellationToken);

        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateKeyException)
        {
            return Result.Failure($"An account with email '{request.Email}' already exists.");
        }

        await TrySendAsync(user.Email.Value, user.FirstName, verificationToken);
        return Result.Success();
    }

    // Sends with a 10-second cap so SMTP timeouts never block the registration response.
    private async Task TrySendAsync(string email, string firstName, string token)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await _emailService.SendEmailVerificationAsync(email, firstName, token, cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[REGISTER] Verification email to {Email} failed: {Error}", email, ex.Message);
        }
    }
}
