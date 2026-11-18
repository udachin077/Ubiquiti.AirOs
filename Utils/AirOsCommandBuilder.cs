namespace Ubiquiti.AirOs.Utils;

/// <summary>
/// Helper methods for building airOS shell commands.
/// </summary>
public static class AirOsCommandBuilder
{
    /// <summary>
    /// Builds a shell command that sets <paramref name="key"/> to <paramref name="value"/>
    /// in <c>/tmp/system.cfg</c>, replacing the existing line if present.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The new value.</param>
    /// <returns>The shell command string.</returns>
    public static string BuildSetCommand(string key, string value)
    {
        string escapedKey = key.Replace("/", "\\/");
        string escapedValue = value?.Replace("/", "\\/") ?? "";
        return $"grep -q '^{key}=' /tmp/system.cfg && " +
               $"sed -i 's/^{key}=.*/{key}={escapedValue}/' /tmp/system.cfg || " +
               $"echo '{key}={escapedValue}' >> /tmp/system.cfg";
    }
}