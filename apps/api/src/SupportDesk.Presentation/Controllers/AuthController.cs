using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Features.Auth.Commands.Login;
using SupportDesk.Application.Features.Auth.Queries.GetCurrentUser;

namespace SupportDesk.Presentation.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    /// <summary>Logs an agent in and issues a signed JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public Task<LoginResponse> Login(
        LoginRequest request,
        [FromServices] LoginCommandHandler handler,
        CancellationToken ct) =>
        handler.HandleAsync(request, ct);

    /// <summary>Returns the current authenticated agent, or 401 Unauthorized.</summary>
    [HttpGet("me")]
    public Task<CurrentUserDto> Me(
        [FromServices] GetCurrentUserQueryHandler handler,
        CancellationToken ct) =>
        handler.HandleAsync(ct);
}
