using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Infrastructure.Configuration;
using SupportDesk.Infrastructure.Services;
using SupportDesk.UnitTests.TestDoubles;
using Xunit;

namespace SupportDesk.UnitTests.Infrastructure.Services;

public sealed class JwtTokenServiceTests
{
    private readonly JwtOptions _options = new()
    {
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        Key = "Test_Secret_Key_At_Least_32_Characters_Long_For_Hmac256!",
        ExpiryMinutes = 60
    };

    [Fact]
    public void GenerateToken_ProducesValidJwtWithExpectedClaimsAndExpiry()
    {
        var clock = new FixedClock(new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc));
        var service = new JwtTokenService(Options.Create(_options), clock);
        var agent = new Agent("Alex Turner", "alex.turner@northwind-support.example", 10, clock.UtcNow, "hash");
        typeof(Agent).GetProperty(nameof(Agent.Id))!.SetValue(agent, 42);

        var (token, expiresAtUtc) = service.GenerateToken(agent);

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(clock.UtcNow.AddMinutes(60), expiresAtUtc);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("TestIssuer", jwt.Issuer);
        Assert.Contains("TestAudience", jwt.Audiences);
        Assert.Equal("42", jwt.Claims.First(c => c.Type is JwtRegisteredClaimNames.Sub or "sub").Value);
        Assert.Equal("alex.turner@northwind-support.example", jwt.Claims.First(c => c.Type is "email" or ClaimTypes.Email).Value);
        Assert.Equal("Alex Turner", jwt.Claims.First(c => c.Type is "unique_name" or "name" or ClaimTypes.Name).Value);
    }
}
