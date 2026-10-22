using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Systems;

public partial class SharedPhysicsSystem
{
    #region AddRemove

    internal void AddAwakeBody(Entity<PhysicsComponent, TransformComponent> ent)
    {
        var body = ent.Comp1;

        if (!body.CanCollide)
        {
            Log.Error($"Tried to add non-colliding {ToPrettyString(ent)} as an awake body to map!");
            DebugTools.Assert(false);
            return;
        }

        if (body.BodyType == BodyType.Static)
        {
            Log.Error($"Tried to add static body {ToPrettyString(ent)} as an awake body to map!");
            DebugTools.Assert(false);
            return;
        }

        DebugTools.Assert(body.Awake);
        DebugTools.Assert(!AwakeBodies.Contains(ent));
        AwakeBodies.Add(ent);
    }

    internal void RemoveSleepBody(Entity<PhysicsComponent, TransformComponent> ent)
    {
        DebugTools.Assert(!ent.Comp1.Awake);
        DebugTools.Assert(AwakeBodies.Contains(ent));
        AwakeBodies.Remove(ent);
    }


    #endregion
}
