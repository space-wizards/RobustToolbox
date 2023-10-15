using JetBrains.Annotations;
using Robust.Shared.Players;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Raised by <see cref="SharedUserInterfaceSystem"/> to check whether an interface is still accessible by its user.
/// </summary>
[ByRefEvent]
[PublicAPI]
public struct BoundUserInterfaceCheckRangeEvent
{
    /// <summary>
    /// The entity owning the UI being checked for.
    /// </summary>
    public readonly EntityUid Target;

    /// <summary>
    /// The UI itself.
    /// </summary>
    /// <returns></returns>
    public readonly PlayerBoundUserInterface UserInterface;

    /// <summary>
    /// The player for which the UI is being checked.
    /// </summary>
    public readonly ICommonSession Player;

    /// <summary>
    /// The result of the range check.
    /// </summary>
    public BoundUserInterfaceRangeResult Result;

    public BoundUserInterfaceCheckRangeEvent(
        EntityUid target,
        PlayerBoundUserInterface userInterface,
        ICommonSession player)
    {
        Target = target;
        UserInterface = userInterface;
        Player = player;
    }
}
