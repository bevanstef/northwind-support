namespace SupportDesk.Application.Contracts.Auth;

/// <summary>Request payload for agent login.</summary>
public sealed record LoginRequest(string Email, string Password);
