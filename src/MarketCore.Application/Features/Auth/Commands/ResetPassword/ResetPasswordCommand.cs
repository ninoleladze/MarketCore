using MediatR;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Auth.Commands.ResetPassword;

public sealed record ResetPasswordCommand(
    string Token,
    string NewPassword) : IRequest<Result>;
