namespace SupportDesk.Application.Abstractions;

/// <summary>
/// Hashes and verifies passwords using a secure, salted hashing algorithm.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a plaintext password.</summary>
    string HashPassword(string password);

    /// <summary>Verifies that a plaintext password matches the hashed password.</summary>
    bool VerifyPassword(string password, string passwordHash);
}
