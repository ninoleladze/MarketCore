namespace MarketCore.Application.Interfaces;

public interface ICurrentUserService
{

    Guid? UserId { get; }

    string? Email { get; }

    bool IsAuthenticated { get; }

    bool IsInRole(string role);
}
