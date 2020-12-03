using Robust.Shared.GameObjects.Systems;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///
    /// </summary>
    public class BroadphaseSystem : EntitySystem
    {
        /*
         * Okay so Aether2D Just uses the DynamicTreeBroadPhase on a per-world basis.
         * For our needs that's not good enough so we're gonna roll our own broadphase.
         * Essentially we need it so when a grid moves none of its entities have their broadphases updated,
         * AKA use relative positioning
         * Worst case we just need to update any entities in our path to be awake and colliding with us (TODO).
         */

        // TODO: Copy the work I'd already done didso under the lookup pr
    }
}
