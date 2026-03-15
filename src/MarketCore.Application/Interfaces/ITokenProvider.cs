using MarketCore.Domain.Entities;

namespace MarketCore.Application.Interfaces;

public interface ITokenProvider
{

    string GenerateToken(User user);
}
