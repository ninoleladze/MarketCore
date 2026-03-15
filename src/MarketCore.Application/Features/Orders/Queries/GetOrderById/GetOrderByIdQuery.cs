using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Orders.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId)
    : IRequest<Result<OrderDto>>, ICacheableQuery
{
    public string CacheKey => $"order:{OrderId}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);
}
