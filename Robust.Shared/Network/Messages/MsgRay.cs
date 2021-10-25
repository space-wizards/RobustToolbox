using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public class MsgRay : EntityEventArgs
    {
        public Vector2 RayOrigin { get; set; }
        public Vector2 RayHit { get; set; }
        public bool DidHit { get; set; }
    }
}
