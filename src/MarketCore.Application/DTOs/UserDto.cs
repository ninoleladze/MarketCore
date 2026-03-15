namespace MarketCore.Application.DTOs;

public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool IsEmailVerified = false);

public sealed record AuthResultDto(
    string Token,
    DateTime ExpiresAt,
    UserDto User);
