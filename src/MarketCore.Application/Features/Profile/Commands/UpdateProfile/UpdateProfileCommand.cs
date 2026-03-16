using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Profile.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string? Street,
    string? City,
    string? State,
    string? ZipCode,
    string? Country) : IRequest<Result<ProfileDto>>;
