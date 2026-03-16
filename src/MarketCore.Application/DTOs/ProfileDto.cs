namespace MarketCore.Application.DTOs;

public sealed record ProfileDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string Role,
    bool IsEmailVerified,
    AddressDto? Address,
    DateTime CreatedAt,
    int TotalOrders,
    decimal TotalSpent);

public sealed record AddressDto(
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country);
