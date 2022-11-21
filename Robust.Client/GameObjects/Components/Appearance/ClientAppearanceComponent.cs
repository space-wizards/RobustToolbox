using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Client.GameObjects;

/// <summary>
/// This is the client instance of <see cref="AppearanceComponent"/>.
/// </summary>
[RegisterComponent]
[ComponentReference(typeof(AppearanceComponent)), Access(typeof(AppearanceSystem))]
public sealed class ClientAppearanceComponent : AppearanceComponent
{
    [DataField("visuals")]
    internal List<AppearanceVisualizer> Visualizers = new();

    /// <summary>
    ///     If true, then this entity's visuals will get updated in the next frame update regardless of whether or not
    ///     this entity is currently inside of PVS range.
    /// </summary>
    /// <remarks>
    ///     This defaults to true, because it is possible for an entity to both be initialized and detached to null
    ///     during the same tick. This can happen because entity states & pvs-departure messages are sent & handled
    ///     separately. However, we want to ensure that each entity has an initial appearance update.
    /// </remarks>
    internal bool UpdateDetached = true;
}
