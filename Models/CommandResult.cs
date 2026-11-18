namespace Ubiquiti.AirOs.Models;

/// <summary>
/// Represents the result of executing a command or a batch of commands on a device.
/// </summary>
/// <param name="Output">The standard output of the command (stdout).</param>
/// <param name="Error">The standard error output (stderr), if any. May be <see langword="null"/>.</param>
/// <param name="ExitStatus">The command exit status. May be <see langword="null"/> if not available.</param>
public sealed record CommandResult(string Output, string? Error, int? ExitStatus)
{
    /// <summary>
    /// Determines whether the command completed successfully.
    /// </summary>
    /// <remarks>
    /// airOS utilities like <c>cfgmtd</c> write progress and status messages to stderr
    /// even on success, so we only trust the exit status when it is available.
    /// </remarks>
    public bool Success => ExitStatus == 0;
}