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
        var alreadyExists = await _uow.Users.ExistsAsync(request.Email, cancellationToken);
        if (alreadyExists)
            return Result.Failure($"An account with email '{request.Email}' already exists.");

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
            return Result.Failure(
                $"An account with email '{request.Email}' already exists.");
        }

        try
        {
            await _emailService.SendEmailVerificationAsync(
                user.Email.Value, user.FirstName, verificationToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[REGISTER] Failed to send verification email to {Email}. User was created successfully.",
                user.Email.Value);
        }

        return Result.Success();
    }
}
