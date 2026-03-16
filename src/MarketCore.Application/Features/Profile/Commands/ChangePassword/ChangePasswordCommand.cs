using MediatR;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Profile.Commands.ChangePassword;

public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword) : IRequest<Result>;
