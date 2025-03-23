using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems;

public partial class SharedPhysicsSystem
{
    #region AddRemove

    internal void AddAwakeBody(EntityUid uid, PhysicsComponent body)
    {
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
        DebugTools.Assert(!AwakeBodies.Contains(body));
        AwakeBodies.Add(body);
    }

    internal void RemoveSleepBody(EntityUid uid, PhysicsComponent body)
    {
        DebugTools.Assert(!body.Awake);
        DebugTools.Assert(AwakeBodies.Contains(body));
        AwakeBodies.Remove(body);
    }


    #endregion
}
