using MediatR;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Auth.Commands.VerifyEmail;

public sealed record VerifyEmailCommand(string Token) : IRequest<Result>;
