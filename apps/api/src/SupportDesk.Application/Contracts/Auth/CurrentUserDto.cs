namespace SupportDesk.Application.Contracts.Auth;

/// <summary>Details of the currently authenticated agent.</summary>
public sealed record CurrentUserDto(
    int Id,
    string FullName,
    string Email,
    bool IsActive);
