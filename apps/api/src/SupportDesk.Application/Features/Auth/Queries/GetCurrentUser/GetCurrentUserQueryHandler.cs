using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Auth.Queries.GetCurrentUser;

/// <summary>
/// Retrieves the profile of the currently authenticated agent.
/// </summary>
public sealed class GetCurrentUserQueryHandler(
    ICurrentUser currentUser,
    IAgentRepository agents)
{
    public async Task<CurrentUserDto> HandleAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.AgentId is not { } agentId)
        {
            throw new UnauthorizedException("User is not authenticated.");
        }

        var agent = await agents.GetByIdAsync(agentId, ct)
            ?? throw new UnauthorizedException("Agent not found.");

        if (!agent.IsActive)
        {
            throw new UnauthorizedException("Agent account is inactive.");
        }

        return new CurrentUserDto(agent.Id, agent.FullName, agent.Email, agent.IsActive);
    }
}
