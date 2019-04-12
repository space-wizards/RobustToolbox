using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects.Components.BoundingBox
{
    /// <summary>
    ///     Holds an Axis Aligned Bounding Box (AABB) for the entity. Using this component adds the entity
    ///     to the physics system as a static (non-movable) entity.
    /// </summary>
    public class ClientBoundingBoxComponent : BoundingBoxComponent
    {
        private Color _debugColor;

        [ViewVariables(VVAccess.ReadWrite)]
        public Color DebugColor
        {
            get => _debugColor;
            set => _debugColor = value;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _debugColor, "DebugColor", Color.Red);
        }
    }
}
