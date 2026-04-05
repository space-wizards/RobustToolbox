namespace Robust.Client.Utility;

[NotContentImplementable]
public interface IPiShockManager
{
    void Initialize();

    /// <summary>
    /// Send an operation to the configured piShock device.
    /// Does nothing if piShock is disabled or credentials are not set.
    /// </summary>
    /// <remarks>
    /// Intensity and duration are clamped to the configured safety caps.
    /// </remarks>
    void TryOperate(PiShockOp op, int intensity, int duration);
}

public enum PiShockOp
{
    Shock = 0,
    Vibrate = 1,
    Beep = 2,
}
