using System;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Map
{
    /// <summary>
    /// A physics shape that represents a <see cref="MapGrid"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public class PhysShapeGrid : IPhysShape
    {
        private GridId _gridId;

        [NonSerialized]
        private IMapGridInternal _mapGrid = default!;

        /// <inheritdoc />
        /// <remarks>
        /// The collision layer of a grid physics shape cannot be changed.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public int CollisionLayer
        {
            get => MapGridHelpers.CollisionGroup;
            set { }
        }

        /// <inheritdoc />
        /// <remarks>
        /// The collision mask of a grid physics shape cannot be changed.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public int CollisionMask
        {
            get => MapGridHelpers.CollisionGroup;
            set { }
        }

        /// <inheritdoc />
        public void ApplyState()
        {
            var mapMan = IoCManager.Resolve<IMapManager>();
            _mapGrid = (IMapGridInternal)mapMan.GetGrid(_gridId);
        }

        public void DebugDraw(DebugDrawingHandle handle, in Matrix3 modelMatrix, in Box2 worldViewport,
            float sleepPercent)
        {
            handle.SetTransform(modelMatrix);
            foreach (var chunk in _mapGrid.GetMapChunks().Values)
            {
                foreach (var box in chunk.CollisionBoxes)
                {
                    var localChunkPos = new Vector2(chunk.Indices.X, chunk.Indices.Y) * _mapGrid.ChunkSize;
                    var localBox = box.Translated(localChunkPos);

                    handle.DrawRect(localBox, handle.CalcWakeColor(handle.GridFillColor, sleepPercent));
                }
            }
            handle.SetTransform(Matrix3.Identity);
        }

        /// <summary>
        /// Constructs a new instance of <see cref="PhysShapeGrid"/>.
        /// </summary>
        public PhysShapeGrid()
        {
            // Better hope ExposeData get called...
        }

        /// <summary>
        /// Constructs a new instance of <see cref="PhysShapeGrid"/>.
        /// </summary>
        public PhysShapeGrid(IMapGrid mapGrid)
        {
            _mapGrid = (IMapGridInternal)mapGrid;
            _gridId = _mapGrid.Index;
        }

        /// <inheritdoc />
        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _gridId, "grid", GridId.Invalid);

            if (serializer.Reading) // There is no Initialize function
            {
                var mapMan = IoCManager.Resolve<IMapManager>();
                _mapGrid = (IMapGridInternal)mapMan.GetGrid(_gridId);
            }
        }

        public event Action? OnDataChanged { add { } remove { } }

        /// <inheritdoc />
        public Box2 CalculateLocalBounds(Angle rotation)
        {
            return new Box2Rotated(_mapGrid.LocalBounds, rotation).CalcBoundingBox();
        }
    }
}
