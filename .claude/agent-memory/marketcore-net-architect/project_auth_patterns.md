---
name: Auth System Patterns
description: Established patterns for authentication, email verification, and password reset in MarketCore
type: project
---

Login requires email verification: `LoginCommandHandler` returns `Result.Failure("Please verify your email address before logging in.")` when `user.IsEmailVerified == false`. This check comes after password verification to avoid leaking whether a user exists.

**Why:** Prevents unverified accounts from accessing the system. The check is after password validation — the same "invalid credentials" error hides both user-not-found and wrong-password cases, but the verified check intentionally surfaces a distinct message so users know to check their inbox.

**How to apply:** Any new login flow must include this gate. Tests using LoginCommandHandler must call `user.MarkEmailVerified()` on success-path users or they will fail with the verification error.

JwtOptions in Application layer: A `JwtOptions` record (`MarketCore.Application.Options.JwtOptions`) mirrors the `ExpiryDays` field from Infrastructure's `JwtSettings`. Both handlers inject `IOptions<JwtOptions>`. Infrastructure's DI extension binds it from the same `"Jwt"` config section alongside `JwtSettings`. This preserves the Application→Domain-only boundary.

**Why:** Application cannot reference Infrastructure. Duplicating only the fields the Application layer needs (`ExpiryDays`) avoids a cross-layer violation without introducing a shared package.

**How to apply:** If new Jwt config fields are needed in Application handlers, add them to `JwtOptions` and rebind in `InfrastructureServiceCollectionExtensions`.

Password reset flow: Token is a 32-char hex GUID (`Guid.NewGuid().ToString("N")`), stored in `User.PasswordResetToken`, expires in 1 hour via `User.PasswordResetTokenExpiresAt`. `ForgotPasswordCommandHandler` always returns `Result.Success()` regardless of whether the email exists (prevents user enumeration). Email is fire-and-forget (`_ = _emailService.SendPasswordResetAsync(..., CancellationToken.None)`).

Rate limiting on auth endpoints: `Register` and `Login` endpoints carry `[EnableRateLimiting("AuthSliding")]` (10 req/5s sliding window). `ForgotPassword` and `ResetPassword` do not carry this attribute.
