using MediatR;
using MarketCore.Application.Exceptions;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;
using MarketCore.Domain.Entities;
using MarketCore.Domain.ValueObjects;
using CartEntity = MarketCore.Domain.Entities.Cart;

namespace MarketCore.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailService _emailService;

    public RegisterCommandHandler(
        IUnitOfWork uow,
        IPasswordHasher passwordHasher,
        IEmailService emailService)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
        _emailService = emailService;
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

        var verificationToken = Guid.NewGuid().ToString("N");
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

        _ = _emailService.SendEmailVerificationAsync(
            user.Email.Value, user.FirstName, verificationToken, CancellationToken.None);

        return Result.Success();
    }
}
