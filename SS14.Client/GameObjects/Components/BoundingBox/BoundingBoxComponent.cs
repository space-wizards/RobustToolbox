using System;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.BoundingBox;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Maths;
using SS14.Shared.Serialization;

namespace SS14.Client.GameObjects
{
    /// <summary>
    ///     Holds an Axis Aligned Bounding Box (AABB) for the entity. Using this component adds the entity
    ///     to the physics system as a static (non-movable) entity.
    /// </summary>
    public class ClientBoundingBoxComponent : BoundingBoxComponent
    {
        private Color _debugColor;

        public Color DebugColor
        {
            get => _debugColor;
            private set => _debugColor = value;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _debugColor, "DebugColor", Color.Red);
        }
    }
}
