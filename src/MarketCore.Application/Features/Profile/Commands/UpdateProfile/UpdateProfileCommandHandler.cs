using MediatR;
using MarketCore.Application.DTOs;
using MarketCore.Application.Interfaces;
using MarketCore.Domain.Common;
using MarketCore.Domain.ValueObjects;

namespace MarketCore.Application.Features.Profile.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<ProfileDto>>
{
    private readonly IUnitOfWork _uow;

    public UpdateProfileCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<ProfileDto>> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _uow.Users.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result<ProfileDto>.Failure("User not found.");

        // Update name fields — domain method enforces validation rules
        var profileResult = user.UpdateProfile(request.FirstName, request.LastName, request.GitHubUrl);
        if (profileResult.IsFailure)
            return Result<ProfileDto>.Failure(profileResult.Error!);

        // Update address only when all address fields are supplied
        var hasAddress = !string.IsNullOrWhiteSpace(request.Street)
            && !string.IsNullOrWhiteSpace(request.City)
            && !string.IsNullOrWhiteSpace(request.State)
            && !string.IsNullOrWhiteSpace(request.ZipCode)
            && !string.IsNullOrWhiteSpace(request.Country);

        if (hasAddress)
        {
            var address = new Address(
                request.Street!,
                request.City!,
                request.State!,
                request.ZipCode!,
                request.Country!);

            var addressResult = user.UpdateAddress(address);
            if (addressResult.IsFailure)
                return Result<ProfileDto>.Failure(addressResult.Error!);
        }

        await _uow.SaveChangesAsync(cancellationToken);

        var orders = await _uow.Orders.GetByUserIdAsync(request.UserId, cancellationToken);
        var orderList = orders.ToList();

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
            GitHubUrl: user.GitHubUrl,
            CreatedAt: user.CreatedAt,
            TotalOrders: orderList.Count,
            TotalSpent: orderList.Sum(o => o.TotalAmount.Amount));

        return Result<ProfileDto>.Success(dto);
    }
}
