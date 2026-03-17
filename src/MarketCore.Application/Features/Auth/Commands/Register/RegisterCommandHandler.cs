using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Application.Exceptions;
using MarketCore.Application.Interfaces;
using MarketCore.Application.Options;
using MarketCore.Domain.Common;
using MarketCore.Domain.Entities;
using MarketCore.Domain.ValueObjects;
using Microsoft.Extensions.Options;
using CartEntity = MarketCore.Domain.Entities.Cart;

namespace MarketCore.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResultDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenProvider _tokenProvider;
    private readonly IEmailService _emailService;
    private readonly JwtOptions _jwtOptions;

    public RegisterCommandHandler(
        IUnitOfWork uow,
        IPasswordHasher passwordHasher,
        ITokenProvider tokenProvider,
        IEmailService emailService,
        IOptions<JwtOptions> jwtOptions)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
        _tokenProvider = tokenProvider;
        _emailService = emailService;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<Result<AuthResultDto>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var alreadyExists = await _uow.Users.ExistsAsync(request.Email, cancellationToken);
        if (alreadyExists)
            return Result<AuthResultDto>.Failure($"An account with email '{request.Email}' already exists.");

        Email emailVo;
        try
        {
            emailVo = new Email(request.Email);
        }
        catch (ArgumentException ex)
        {
            return Result<AuthResultDto>.Failure(ex.Message);
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

            return Result<AuthResultDto>.Failure(
                $"An account with email '{request.Email}' already exists.");
        }

        var jwtToken  = _tokenProvider.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddDays(_jwtOptions.ExpiryDays);

        var verificationUrl =
            $"{request.ClientBaseUrl?.TrimEnd('/')}/auth/verify-email?token={verificationToken}";

        _ = _emailService.SendEmailVerificationAsync(
            user.Email.Value, user.FirstName, verificationUrl, CancellationToken.None);

        var dto = BuildAuthResult(user, jwtToken, expiresAt);
        return Result<AuthResultDto>.Success(dto);
    }

    private static AuthResultDto BuildAuthResult(User user, string token, DateTime expiresAt) =>
        new(
            Token: token,
            ExpiresAt: expiresAt,
            User: new UserDto(
                Id: user.Id,
                Email: user.Email.Value,
                FirstName: user.FirstName,
                LastName: user.LastName,
                Role: user.Role.ToString(),
                IsEmailVerified: user.IsEmailVerified));
}
