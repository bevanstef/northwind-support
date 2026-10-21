namespace SupportDesk.Application.Abstractions;

/// <summary>
/// Provides access to the currently authenticated agent's identity extracted from the JWT token.
/// </summary>
public interface ICurrentUser
{
    int? AgentId { get; }

    string? FullName { get; }

    string? Email { get; }

    bool IsAuthenticated { get; }
}
