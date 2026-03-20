using MediatR;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? ClientBaseUrl = null) : IRequest<Result>;
