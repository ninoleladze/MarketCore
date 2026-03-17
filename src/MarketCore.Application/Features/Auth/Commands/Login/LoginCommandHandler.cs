using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Application.Interfaces;
using MarketCore.Application.Options;
using MarketCore.Domain.Common;
using Microsoft.Extensions.Options;

namespace MarketCore.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResultDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenProvider _tokenProvider;
    private readonly JwtOptions _jwtOptions;

    public LoginCommandHandler(
        IUnitOfWork uow,
        IPasswordHasher passwordHasher,
        ITokenProvider tokenProvider,
        IOptions<JwtOptions> jwtOptions)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
        _tokenProvider = tokenProvider;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<Result<AuthResultDto>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _uow.Users.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<AuthResultDto>.Failure("Invalid email or password.");

        if (!user.IsEmailVerified)
            return Result<AuthResultDto>.Failure("Please verify your email address before logging in.");

        var token = _tokenProvider.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddDays(_jwtOptions.ExpiryDays);

        var dto = new AuthResultDto(
            Token: token,
            ExpiresAt: expiresAt,
            User: new UserDto(
                Id: user.Id,
                Email: user.Email.Value,
                FirstName: user.FirstName,
                LastName: user.LastName,
                Role: user.Role.ToString(),
                IsEmailVerified: user.IsEmailVerified));

        return Result<AuthResultDto>.Success(dto);
    }
}
