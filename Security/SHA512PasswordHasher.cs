using System.Security.Cryptography;
using CryptSharp;
using Ubiquiti.AirOs.Abstractions;

namespace Ubiquiti.AirOs.Security;

/// <summary>
/// Implements password hashing using SHA-512 with salt.
/// Produces hashes compatible with the /etc/shadow format ($6$salt$hash).
/// </summary>
public class SHA512PasswordHasher : IUbiquitiPasswordHasher
{
    private const int SaltBytes = 8;

    /// <summary>
    /// Hashes the specified password using SHA-512 with a randomly generated salt.
    /// </summary>
    /// <param name="password">The plain text password to hash.</param>
    /// <returns>The hashed password in $6$salt$hash format.</returns>
    /// <exception cref="ArgumentNullException">Thrown when password is <see langword="null"/>.</exception>
    public string HashPassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password, nameof(password));

        var saltBytes = RandomNumberGenerator.GetBytes(SaltBytes);
        var saltHex = Convert.ToHexString(saltBytes).ToLowerInvariant();
        string salt = $"$6${saltHex}";
        return Crypter.Sha512.Crypt(password, salt);
    }
}