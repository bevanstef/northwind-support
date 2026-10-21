namespace SupportDesk.Application.Contracts.Auth;

/// <summary>Successful login response containing JWT, expiry, and current user info.</summary>
public sealed record LoginResponse(
    string Token,
    DateTime ExpiresAtUtc,
    CurrentUserDto User);
