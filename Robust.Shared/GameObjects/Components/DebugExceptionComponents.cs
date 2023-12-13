namespace Robust.Shared.GameObjects
{
    // If you wanna use these, add it to some random prototype.
    // I recommend the #1 mug:
    // 1. it doesn't spawn on the map (currently).
    // 2. it's at the top of the entity list (currently).
    // 3. it crashed the game before, it's iconic!

    /// <summary>
    /// Throws an exception in <see cref="OnAdd" />.
    /// </summary>
    [RegisterComponent]
    public sealed partial class DebugExceptionOnAddComponent : Component { }

    /// <summary>
    /// Throws an exception in <see cref="Initialize" />.
    /// </summary>
    [RegisterComponent]
    public sealed partial class DebugExceptionInitializeComponent : Component { }

    /// <summary>
    /// Throws an exception in <see cref="Startup" />.
    /// </summary>
    [RegisterComponent]
    public sealed partial class DebugExceptionStartupComponent : Component { }
}
