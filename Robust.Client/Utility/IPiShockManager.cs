using System;

namespace Robust.Client.Utility;

[NotContentImplementable]
public interface IPiShockManager
{
    void Initialize();

    /// <remarks>
    /// op, intensity (1–100), duration (1–15 seconds).
    /// </remarks>
    Action<PiShockOp, int, int> Operate { get; }
}

public enum PiShockOp
{
    Shock = 0,
    Vibrate = 1,
    Beep = 2,
}
