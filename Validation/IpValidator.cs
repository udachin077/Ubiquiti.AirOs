using System.Net;
using System.Net.Sockets;
using Ubiquiti.AirOs.Abstractions;

namespace Ubiquiti.AirOs.Validation;

/// <summary>
/// Default implementation of <see cref="IIpValidator"/> using <see cref="IPAddress"/> with strict validation.
/// </summary>
public class IpValidator : IIpValidator
{
    /// <inheritdoc />
    public bool IsValidIpAddress(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return false;

        if (!IPAddress.TryParse(ip, out var address) || address.AddressFamily != AddressFamily.InterNetwork)
            return false;

        byte[] bytes = address.GetAddressBytes();
        uint ipUint = (uint)bytes[0] << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3];

        if (ipUint == 0) return false;
        if (ipUint == 0xFFFFFFFF) return false;
        if ((ipUint >> 24) == 127) return false;
        if ((ipUint >> 28) == 0xE) return false;
        if ((ipUint >> 16) == 0xA9FE) return false;

        return true;
    }

    /// <inheritdoc />
    public bool IsValidNetmask(string netmask)
    {
        if (string.IsNullOrWhiteSpace(netmask))
            return false;

        if (!IPAddress.TryParse(netmask, out var mask) || mask.AddressFamily != AddressFamily.InterNetwork)
            return false;

        byte[] bytes = mask.GetAddressBytes();
        uint maskUint = (uint)bytes[0] << 24 | (uint)bytes[1] << 16 | (uint)bytes[2] << 8 | bytes[3];

        if (maskUint == 0)
            return false;

        string binary = Convert.ToString(maskUint, 2).PadLeft(32, '0');
        bool seenZero = false;
        foreach (char c in binary)
        {
            if (c == '0')
                seenZero = true;
            else if (seenZero && c == '1')
                return false;
        }
        return true;
    }

    /// <inheritdoc />
    public bool IsValidGateway(string gateway)
    {
        if (string.IsNullOrWhiteSpace(gateway))
            return false;

        if (!IsValidIpAddress(gateway))
            return false;

        if (gateway == "0.0.0.0")
            return false;

        return true;
    }
}