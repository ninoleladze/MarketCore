using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;

namespace MarketCore.Application.Features.Profile.Queries.GetProfile;

public sealed class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, Result<ProfileDto>>
{
    private readonly IUnitOfWork _uow;

    public GetProfileQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<ProfileDto>> Handle(
        GetProfileQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _uow.Users.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result<ProfileDto>.Failure("User not found.");

        var orders = await _uow.Orders.GetByUserIdAsync(request.UserId, cancellationToken);
        var orderList = orders.ToList();

        var totalOrders = orderList.Count;
        var totalSpent = orderList.Sum(o => o.TotalAmount.Amount);

        AddressDto? addressDto = user.Address is null
            ? null
            : new AddressDto(
                user.Address.Street,
                user.Address.City,
                user.Address.State,
                user.Address.ZipCode,
                user.Address.Country);

        var dto = new ProfileDto(
            Id: user.Id,
            Email: user.Email.Value,
            FirstName: user.FirstName,
            LastName: user.LastName,
            FullName: user.FullName,
            Role: user.Role.ToString(),
            IsEmailVerified: user.IsEmailVerified,
            Address: addressDto,
            CreatedAt: user.CreatedAt,
            TotalOrders: totalOrders,
            TotalSpent: totalSpent);

        return Result<ProfileDto>.Success(dto);
    }
}
