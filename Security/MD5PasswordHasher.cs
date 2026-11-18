using CryptSharp;
using Ubiquiti.AirOs.Abstractions;

namespace Ubiquiti.AirOs.Security;

/// <summary>
/// Implements password hashing using MD5.
/// </summary>
public class MD5PasswordHasher : IUbiquitiPasswordHasher
{
    /// <summary>
    /// Hashes the specified password using MD5.
    /// </summary>
    /// <param name="password">The plain text password to hash.</param>
    /// <returns>The hashed password in $1$hash format.</returns>
    /// <exception cref="ArgumentNullException">Thrown when password is <see langword="null"/>.</exception>
    public string HashPassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password, nameof(password));
        return Crypter.MD5.Crypt(password);
    }
}