using MediatR;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Auth.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(
    string Email,
    string? ClientBaseUrl = null) : IRequest<Result>;
