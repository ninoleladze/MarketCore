using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResultDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenProvider _tokenProvider;

    public LoginCommandHandler(
        IUnitOfWork uow,
        IPasswordHasher passwordHasher,
        ITokenProvider tokenProvider)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
        _tokenProvider = tokenProvider;
    }

    public async Task<Result<AuthResultDto>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _uow.Users.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<AuthResultDto>.Failure("Invalid email or password.");

        var token = _tokenProvider.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddDays(7);

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
