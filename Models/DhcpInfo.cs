namespace Ubiquiti.AirOs.Models;

/// <summary>
/// Represents the DHCP lease information obtained from the device.
/// </summary>
public class DhcpInfo
{
    /// <summary>Gets or sets the network interface name (e.g., "br0").</summary>
    public string Interface { get; set; } = string.Empty;

    /// <summary>Gets or sets the broadcast address.</summary>
    public string? Broadcast { get; set; }

    /// <summary>Gets or sets the subnet mask.</summary>
    public string? Netmask { get; set; }

    /// <summary>Gets or sets the assigned IP address.</summary>
    public string? Ip { get; set; }

    /// <summary>Gets or sets the default gateway.</summary>
    public string? Gateway { get; set; }

    /// <summary>Gets or sets the array of DNS servers provided by DHCP.</summary>
    public string[] DnsServers { get; set; } = Array.Empty<string>();

    /// <summary>Gets or sets the hostname provided by DHCP.</summary>
    public string? Hostname { get; set; }

    /// <summary>Gets or sets the DHCP server IP address.</summary>
    public string? DhcpIp { get; set; }

    /// <summary>Gets or sets the domain name provided by DHCP.</summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Gets the primary DNS server, if available.
    /// </summary>
    public string? PrimaryDns => DnsServers.Length > 0 ? DnsServers[0] : null;

    /// <summary>
    /// Gets the secondary DNS server, if available.
    /// </summary>
    public string? SecondaryDns => DnsServers.Length > 1 ? DnsServers[1] : null;
}