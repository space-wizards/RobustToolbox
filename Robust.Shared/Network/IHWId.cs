using System;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Shared.Network;

/// <summary>
/// Fetches HWID (hardware ID) unique identifiers for the local system.
/// </summary>
internal interface IHWId
{
    /// <summary>
    /// Gets the "legacy" HWID.
    /// </summary>
    /// <remarks>
    /// These are directly sent to servers and therefore susceptible to malicious spoofing.
    /// They should not be relied on for the future.
    /// </remarks>
    /// <returns>
    /// An opaque value that gets sent to the server to identify this computer,
    /// or an empty array if legacy HWID is not supported on this platform.
    /// </returns>
    byte[] GetLegacy();

    /// <summary>
    /// Gets the "modern" HWID.
    /// </summary>
    /// <returns>
    /// An opaque value that gets sent to the auth server to identify this computer,
    /// or null if modern HWID is not supported on this platform.
    /// </returns>
    byte[]? GetModern();
}

/// <summary>
/// Implementation of <see cref="IHWId"/> that does nothing, always returning an empty result.
/// </summary>
internal sealed class DummyHWId : IHWId
{
    public byte[] GetLegacy()
    {
        return [];
    }

    public byte[] GetModern()
    {
        return [];
    }
}

#if DEBUG
internal sealed class HwidCommand : LocalizedCommands
{
    [Dependency] private readonly IHWId _hwId = default!;

    public override string Command => "hwid";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        shell.WriteLine($"""
            legacy: {Convert.ToBase64String(_hwId.GetLegacy(), Base64FormattingOptions.None)}
            modern: {Base64Helpers.ToBase64Nullable(_hwId.GetModern())}
            """);
    }
}
#endif
