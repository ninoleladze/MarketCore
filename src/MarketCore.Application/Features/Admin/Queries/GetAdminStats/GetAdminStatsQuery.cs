using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Admin.Queries.GetAdminStats;

public sealed record GetAdminStatsQuery : IRequest<Result<AdminStatsDto>>;
