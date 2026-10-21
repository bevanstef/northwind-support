using SupportDesk.Infrastructure.Services;
using Xunit;

namespace SupportDesk.UnitTests.Infrastructure.Services;

public sealed class IdentityPasswordHasherTests
{
    private readonly IdentityPasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ProducesSaltedHashDifferentOnEachCall()
    {
        const string password = "DevPassword123!";

        var hash1 = _hasher.HashPassword(password);
        var hash2 = _hasher.HashPassword(password);

        Assert.False(string.IsNullOrWhiteSpace(hash1));
        Assert.False(string.IsNullOrWhiteSpace(hash2));
        Assert.NotEqual(hash1, hash2); // Salt ensures distinct hashes
    }

    [Fact]
    public void VerifyPassword_ReturnsTrue_ForCorrectPassword()
    {
        const string password = "DevPassword123!";
        var hash = _hasher.HashPassword(password);

        var result = _hasher.VerifyPassword(password, hash);

        Assert.True(result);
    }

    [Theory]
    [InlineData("WrongPassword!")]
    [InlineData("devpassword123!")]
    [InlineData("")]
    [InlineData(" ")]
    public void VerifyPassword_ReturnsFalse_ForIncorrectOrEmptyPassword(string wrongPassword)
    {
        const string password = "DevPassword123!";
        var hash = _hasher.HashPassword(password);

        var result = _hasher.VerifyPassword(wrongPassword, hash);

        Assert.False(result);
    }
}
