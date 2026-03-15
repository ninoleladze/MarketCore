using MarketCore.Application.Interfaces;
using MarketCore.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using AppPasswordHasher = MarketCore.Application.Interfaces.IPasswordHasher;

namespace MarketCore.Infrastructure.Auth;

public sealed class PasswordHasher : AppPasswordHasher
{

    private static readonly PasswordHasher<User> IdentityHasher = new();

    public string Hash(string password)
    {
        return IdentityHasher.HashPassword(null!, password);
    }

    public bool Verify(string password, string hash)
    {
        var result = IdentityHasher.VerifyHashedPassword(null!, hash, password);

        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
