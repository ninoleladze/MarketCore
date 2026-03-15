using MarketCore.Domain.Common;
using MarketCore.Domain.Enums;
using MarketCore.Domain.ValueObjects;

namespace MarketCore.Domain.Entities;

public sealed class User : AggregateRoot
{

    public Email Email { get; private set; } = null!;

    public string PasswordHash { get; private set; } = string.Empty;

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public Address? Address { get; private set; }

    public bool IsEmailVerified { get; private set; }

    public string? EmailVerificationToken { get; private set; }

    public Cart? Cart { get; private set; }

    private User() { }

    private User(Email email, string passwordHash, string firstName, string lastName, UserRole role) : base()
    {
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        Role = role;
    }

    public static User Create(
        Email email,
        string passwordHash,
        string firstName,
        string lastName,
        UserRole role = UserRole.Customer)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));

        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name cannot be empty.", nameof(firstName));

        if (firstName.Length > 100)
            throw new ArgumentException("First name cannot exceed 100 characters.", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name cannot be empty.", nameof(lastName));

        if (lastName.Length > 100)
            throw new ArgumentException("Last name cannot exceed 100 characters.", nameof(lastName));

        return new User(email, passwordHash, firstName.Trim(), lastName.Trim(), role);
    }

    public Result ChangePassword(string newHash)
    {
        if (string.IsNullOrWhiteSpace(newHash))
            return Result.Failure("New password hash cannot be empty.");

        PasswordHash = newHash;
        return Result.Success();
    }

    public Result UpdateAddress(Address address)
    {
        if (address is null)
            return Result.Failure("Address cannot be null.");

        Address = address;
        return Result.Success();
    }

    public Result UpdateProfile(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return Result.Failure("First name cannot be empty.");

        if (firstName.Length > 100)
            return Result.Failure("First name cannot exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(lastName))
            return Result.Failure("Last name cannot be empty.");

        if (lastName.Length > 100)
            return Result.Failure("Last name cannot exceed 100 characters.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        return Result.Success();
    }

    public string FullName => $"{FirstName} {LastName}";

    public void SetVerificationToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Verification token cannot be empty.", nameof(token));

        EmailVerificationToken = token;
        IsEmailVerified = false;
    }

    public Result ConfirmEmail(string token)
    {
        if (IsEmailVerified)
            return Result.Failure("Email is already verified.");

        if (string.IsNullOrWhiteSpace(token) || EmailVerificationToken != token)
            return Result.Failure("Invalid or expired verification token.");

        IsEmailVerified = true;
        EmailVerificationToken = null;
        return Result.Success();
    }

    public void MarkEmailVerified()
    {
        IsEmailVerified = true;
        EmailVerificationToken = null;
    }
}
