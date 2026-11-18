using Ubiquiti.AirOs.Models;

namespace Ubiquiti.AirOs.Utils;

public static class DhcpInfoHelper
{
    public static DhcpInfo ParseDhcpInfo(string raw)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            var eqIndex = trimmed.IndexOf('=');
            if (eqIndex < 1)
                continue;

            var key = trimmed[..eqIndex];
            var value = trimmed[(eqIndex + 1)..].Trim().Trim('"');

            dict[key] = value;
        }

        return new DhcpInfo
        {
            Interface = dict.GetValueOrDefault("u_interface", string.Empty),
            Broadcast = NullIfEmpty(dict.GetValueOrDefault("u_broadcast")),
            Netmask = NullIfEmpty(dict.GetValueOrDefault("u_subnet")),
            Ip = NullIfEmpty(dict.GetValueOrDefault("u_ip")),
            Gateway = NullIfEmpty(dict.GetValueOrDefault("u_router")),
            DnsServers = (dict.GetValueOrDefault("u_dns") ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries),
            DhcpIp = NullIfEmpty(dict.GetValueOrDefault("u_serverid"))
        };
    }
    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    private static int? ParseInt(string? value)
        => int.TryParse(value, out var v) ? v : null;

    private static long? ParseLong(string? value)
        => long.TryParse(value, out var v) ? v : null;
}
