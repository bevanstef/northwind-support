using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SupportDesk.Application.Abstractions;

namespace SupportDesk.Infrastructure.Services;

/// <summary>
/// Resolves the current authenticated user's claims from the active HTTP request context.
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public int? AgentId
    {
        get
        {
            var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User?.FindFirst("sub")?.Value;

            return int.TryParse(idClaim, out var id) ? id : null;
        }
    }

    public string? FullName =>
        User?.FindFirst(ClaimTypes.Name)?.Value
        ?? User?.FindFirst("unique_name")?.Value
        ?? User?.FindFirst("name")?.Value;

    public string? Email =>
        User?.FindFirst(ClaimTypes.Email)?.Value
        ?? User?.FindFirst("email")?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
