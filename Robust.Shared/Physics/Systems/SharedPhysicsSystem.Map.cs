using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems;

public partial class SharedPhysicsSystem
{
    #region AddRemove

    internal void AddAwakeBody(EntityUid uid, PhysicsComponent body, PhysicsMapComponent? map = null)
    {
        if (map == null)
            return;

        if (!body.CanCollide)
        {
            Log.Error($"Tried to add non-colliding {ToPrettyString(uid)} as an awake body to map!");
            DebugTools.Assert(false);
            return;
        }

        if (body.BodyType == BodyType.Static)
        {
            Log.Error($"Tried to add static body {ToPrettyString(uid)} as an awake body to map!");
            DebugTools.Assert(false);
            return;
        }

        DebugTools.Assert(body.Awake);
        map.AwakeBodies.Add(body);
    }

    internal void AddAwakeBody(EntityUid uid, PhysicsComponent body, EntityUid mapUid, PhysicsMapComponent? map = null)
    {
        PhysMapQuery.Resolve(mapUid, ref map, false);
        AddAwakeBody(uid, body, map);
    }

    internal void RemoveSleepBody(EntityUid uid, PhysicsComponent body, PhysicsMapComponent? map = null)
    {
        map?.AwakeBodies.Remove(body);
    }

    internal void RemoveSleepBody(EntityUid uid, PhysicsComponent body, EntityUid mapUid, PhysicsMapComponent? map = null)
    {
        PhysMapQuery.Resolve(mapUid, ref map, false);
        RemoveSleepBody(uid, body, map);
    }

    #endregion
}
