using Microsoft.Extensions.Logging;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Auth.Commands.Login;

/// <summary>
/// Authenticates an agent using email and password, issuing a signed JWT on success.
/// </summary>
public sealed class LoginCommandHandler(
    IAgentRepository agents,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtGenerator,
    ILogger<LoginCommandHandler> logger)
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    // Dummy hash formatted like Identity v3 (PBKDF2 HMAC-SHA256) used to mitigate user enumeration timing attacks.
    private const string DummyHash = "AQAAAAIAAYagAAAAEP0wN5i9L6NqZ8eKk3q0yL7k5v9L6NqZ8eKk3q0yL7k5v9==";

    public async Task<LoginResponse> HandleAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var agent = await agents.GetByEmailAsync(email, ct);

        if (agent is null)
        {
            // Execute dummy verify so the request takes similar computation time as a valid user check.
            passwordHasher.VerifyPassword(request.Password, DummyHash);
            logger.LogWarning("Login failed: unknown email {Email}.", email);
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        if (!agent.IsActive)
        {
            // Still perform a hash check so inactive accounts cannot be enumerated by timing differences.
            passwordHasher.VerifyPassword(request.Password, agent.PasswordHash);
            logger.LogWarning("Login failed: agent {Email} is inactive.", email);
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        if (!passwordHasher.VerifyPassword(request.Password, agent.PasswordHash))
        {
            logger.LogWarning("Login failed: invalid password for {Email}.", email);
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        var (token, expiresAtUtc) = jwtGenerator.GenerateToken(agent);

        logger.LogInformation("Agent {AgentId} ({Email}) logged in successfully.", agent.Id, agent.Email);

        return new LoginResponse(
            token,
            expiresAtUtc,
            new CurrentUserDto(agent.Id, agent.FullName, agent.Email, agent.IsActive));
    }
}
