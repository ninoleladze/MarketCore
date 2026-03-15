using FluentValidation;

namespace MarketCore.Application.Features.Admin.Queries.GetAdminStats;

public sealed class GetAdminStatsQueryValidator : AbstractValidator<GetAdminStatsQuery>
{
    public GetAdminStatsQueryValidator()
    {

    }
}
