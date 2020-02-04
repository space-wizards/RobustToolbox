using Robust.Client.Graphics.ClientEye;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
{
    public class OccluderComponent : Component
    {
        public override string Name => "Occluder";

        [ViewVariables]
        public Box2 BoundingBox { get; private set; } = new Box2(-0.5f, -0.5f, 0.5f, 0.5f);

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        private bool _enabled = true;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _enabled, "enabled", true);
        }
    }
}
