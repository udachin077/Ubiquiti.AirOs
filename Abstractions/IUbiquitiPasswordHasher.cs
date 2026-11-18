namespace Ubiquiti.AirOs.Abstractions;

/// <summary>
/// Defines a contract for password hashing implementations.
/// </summary>
public interface IUbiquitiPasswordHasher
{
    /// <summary>
    /// Hashes the specified password using the implemented algorithm.
    /// </summary>
    /// <param name="password">The plain text password to hash.</param>
    /// <returns>The hashed password string.</returns>
    string HashPassword(string password);
}