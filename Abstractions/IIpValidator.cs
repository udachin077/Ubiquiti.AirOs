namespace Ubiquiti.AirOs.Abstractions;

/// <summary>
/// Defines methods for validating IP addresses and network masks.
/// </summary>
public interface IIpValidator
{
    /// <summary>
    /// Validates that the specified string is a valid IPv4 address.
    /// </summary>
    /// <param name="ip">The IP address string to validate.</param>
    /// <returns><see langword="true"/> if the address is valid; otherwise, <see langword="false"/>.</returns>
    bool IsValidIpAddress(string ip);

    /// <summary>
    /// Validates that the specified string is a valid IPv4 network mask.
    /// </summary>
    /// <param name="netmask">The network mask string to validate.</param>
    /// <returns><see langword="true"/> if the mask is valid; otherwise, <see langword="false"/>.</returns>
    bool IsValidNetmask(string netmask);

    /// <summary>
    /// Validates that the specified string is a valid IPv4 gateway address.
    /// </summary>
    /// <param name="gateway">The gateway address string to validate.</param>
    /// <returns><see langword="true"/> if the gateway is valid; otherwise, <see langword="false"/>.</returns>
    bool IsValidGateway(string gateway);
}