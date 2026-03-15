using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Admin.Queries.GetAdminStats;

public sealed class GetAdminStatsQueryHandler
    : IRequestHandler<GetAdminStatsQuery, Result<AdminStatsDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public GetAdminStatsQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Result<AdminStatsDto>> Handle(
        GetAdminStatsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsInRole("Admin"))
            return Result<AdminStatsDto>.Failure(
                "Only administrators are authorised to view platform statistics.");

        var totalProducts = await _uow.Products.CountAllAsync(cancellationToken);
        var totalUsers    = await _uow.Users.CountAllAsync(cancellationToken);
        var totalOrders   = await _uow.Orders.CountAllAsync(cancellationToken);
        var totalRevenue  = await _uow.Orders.GetTotalRevenueAsync(cancellationToken);

        var dto = new AdminStatsDto(
            TotalProducts: totalProducts,
            TotalUsers: totalUsers,
            TotalOrders: totalOrders,
            TotalRevenue: totalRevenue);

        return Result<AdminStatsDto>.Success(dto);
    }
}
