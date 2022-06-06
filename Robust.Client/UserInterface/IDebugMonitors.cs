namespace Robust.Client.UserInterface;

/// <summary>
/// Manages the debug monitors overlay, AKA "F3 screen".
/// </summary>
public interface IDebugMonitors
{
    /// <summary>
    /// Whether debug monitors are currently visible.
    /// </summary>
    bool Visible { get; set; }

    /// <summary>
    /// Toggle visibility of a specific debug monitor.
    /// </summary>
    void ToggleMonitor(DebugMonitor monitor);

    /// <summary>
    /// Set visibility of a specific debug monitor.
    /// </summary>
    void SetMonitor(DebugMonitor monitor, bool visible);
}

/// <summary>
/// Debug monitors available in the debug monitors overlay.
/// </summary>
public enum DebugMonitor
{
    Fps,
    Coords,
    Net,
    Time,
    Frames,
    Memory,
    Clyde,
    Input,
    Bandwidth,
    Prof
}
