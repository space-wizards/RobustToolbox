using System.Diagnostics;

namespace Robust.Packaging.Utility;

/// <summary>
/// Helpers for working with <see cref="Process"/>.
/// </summary>
public static class ProcessHelpers
{
    public static async Task RunCheck(ProcessStartInfo info)
    {
        var process = Process.Start(info);
        if (process == null)
            throw new SubprocessException($"Failed to start subprocess {info.FileName}");

        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new SubprocessException($"Subprocess {info.FileName} failed with code: {process.ExitCode}");
    }
}

/// <summary>
/// Thrown if an error occured in a sub-process.
/// </summary>
public sealed class SubprocessException : Exception
{
    public SubprocessException(string message)
    {

    }
}
