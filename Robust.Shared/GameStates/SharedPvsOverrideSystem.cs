using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Robust.Shared.GameStates;

public abstract class SharedPvsOverrideSystem : EntitySystem
{
    /// <summary>
    /// Forces the entity, all of its parents, and all of its children to ignore normal PVS range limitations,
    /// causing them to always be sent to all clients.
    /// </summary>
    public virtual void AddGlobalOverride(EntityUid uid)
    {

    }

    /// <summary>
    /// Removes an entity from the global overrides.
    /// </summary>
    public virtual void RemoveGlobalOverride(EntityUid uid)
    {

    }

    /// <summary>
    /// Forces the entity, all of its parents, and all of its children to ignore normal PVS range limitations for a
    /// specific session.
    /// </summary>
    public virtual void AddSessionOverride(EntityUid uid, ICommonSession session)
    {

    }

    /// <summary>
    /// Removes an entity from a session's overrides.
    /// </summary>
    public virtual void RemoveSessionOverride(EntityUid uid, ICommonSession session)
    {

    }

    /// <summary>
    /// Forces the entity, all of its parents, and all of its children to ignore normal PVS range limitations,
    /// causing them to always be sent to all clients.
    /// </summary>
    public virtual void AddSessionOverrides(EntityUid uid, Filter filter)
    {

    }
}
