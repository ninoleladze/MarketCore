using FluentAssertions;
using MarketCore.Application.Features.Auth.Commands.Login;
using MarketCore.Application.Interfaces;
using MarketCore.Application.Options;
using MarketCore.Domain.Entities;
using MarketCore.Domain.Enums;
using MarketCore.Domain.Repositories;
using MarketCore.Domain.ValueObjects;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace MarketCore.Tests.Application;

/// <summary>
/// Unit tests for LoginCommandHandler.
/// Layer: MarketCore.Tests
///
/// Tests the authentication flow: user lookup, password verification,
/// email verification gate, and token generation.
/// All external dependencies are mocked via NSubstitute.
/// </summary>
public sealed class LoginCommandHandlerTests
{
    private const string RawPassword = "TestPassword123!";
    private const string FakeHash = "AQAAAAEAACcQAAAAEFakeHashedPasswordForTestingOnly==";
    private const string FakeToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.fake.token";

    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenProvider _tokenProvider;
    private readonly IUserRepository _userRepo;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _uow = Substitute.For<IUnitOfWork>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _tokenProvider = Substitute.For<ITokenProvider>();
        _userRepo = Substitute.For<IUserRepository>();

        _uow.Users.Returns(_userRepo);

        var jwtOptions = Options.Create(new JwtOptions { ExpiryDays = 7 });
        _handler = new LoginCommandHandler(_uow, _passwordHasher, _tokenProvider, jwtOptions);
    }

    private static User BuildUnverifiedUser()
    {
        return User.Create(
            new Email("user@example.com"),
            FakeHash,
            "Alice",
            "Example",
            UserRole.Customer);
    }

    private static User BuildVerifiedUser()
    {
        var user = User.Create(
            new Email("user@example.com"),
            FakeHash,
            "Alice",
            "Example",
            UserRole.Customer);
        user.MarkEmailVerified();
        return user;
    }

    // ── User not found ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UserNotFound_ReturnsGenericFailure()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _handler.Handle(
            new LoginCommand("unknown@example.com", RawPassword),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid email or password.");
    }

    // ── Wrong password ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WrongPassword_ReturnsGenericFailure()
    {
        var user = BuildVerifiedUser();
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        var result = await _handler.Handle(
            new LoginCommand("user@example.com", "WrongPassword!"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        // Same error as user-not-found to prevent user enumeration.
        result.Error.Should().Be("Invalid email or password.");
    }

    // ── Email not verified gate ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_EmailNotVerified_ReturnsVerificationFailure()
    {
        var user = BuildUnverifiedUser();
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        _passwordHasher.Verify(RawPassword, FakeHash).Returns(true);

        var result = await _handler.Handle(
            new LoginCommand("user@example.com", RawPassword),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Please verify your email address before logging in.");

        _tokenProvider.DidNotReceive().GenerateToken(Arg.Any<User>());
    }

    // ── Successful login ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsSuccessWithToken()
    {
        var user = BuildVerifiedUser();
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        _passwordHasher.Verify(RawPassword, FakeHash).Returns(true);
        _tokenProvider.GenerateToken(user).Returns(FakeToken);

        var result = await _handler.Handle(
            new LoginCommand("user@example.com", RawPassword),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().Be(FakeToken);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsCorrectUserProfile()
    {
        var user = BuildVerifiedUser();
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        _passwordHasher.Verify(RawPassword, FakeHash).Returns(true);
        _tokenProvider.GenerateToken(user).Returns(FakeToken);

        var result = await _handler.Handle(
            new LoginCommand("user@example.com", RawPassword),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value!;
        dto.User.Email.Should().Be("user@example.com");
        dto.User.FirstName.Should().Be("Alice");
        dto.User.LastName.Should().Be("Example");
        dto.User.Role.Should().Be("Customer");
        dto.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_ValidCredentials_DoesNotGenerateTokenForFailedVerification()
    {
        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        await _handler.Handle(
            new LoginCommand("ghost@example.com", RawPassword),
            CancellationToken.None);

        // Token must never be generated when credentials are invalid.
        _tokenProvider.DidNotReceive().GenerateToken(Arg.Any<User>());
    }

    // ── Enum-to-string mapping ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_AdminUser_ReturnsAdminRoleInDto()
    {
        var admin = User.Create(
            new Email("admin@example.com"),
            FakeHash,
            "Adam",
            "Admin",
            UserRole.Admin);
        admin.MarkEmailVerified();

        _userRepo.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(admin);

        _passwordHasher.Verify(RawPassword, FakeHash).Returns(true);
        _tokenProvider.GenerateToken(admin).Returns(FakeToken);

        var result = await _handler.Handle(
            new LoginCommand("admin@example.com", RawPassword),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.User.Role.Should().Be("Admin");
    }
}
