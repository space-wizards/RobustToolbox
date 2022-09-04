using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Shared.Map
{
    public struct FindGridsEnumerator : IDisposable
    {
        private readonly IEntityManager _entityManager;

        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private IEnumerator<MapGridComponent> _enumerator;

        private MapId _mapId;
        private Box2 _worldAABB;
        private bool _approx;

        internal FindGridsEnumerator(IEntityManager entityManager, IEnumerator<MapGridComponent> enumerator, MapId mapId, Box2 worldAABB, bool approx)
        {
            _entityManager = entityManager;
            _enumerator = enumerator;
            _mapId = mapId;
            _worldAABB = worldAABB;
            _approx = approx;
        }

        public bool MoveNext([NotNullWhen(true)] out MapGridComponent? grid)
        {
            while (true)
            {
                if (!_enumerator.MoveNext())
                {
                    grid = null;
                    return false;
                }

                var nextGrid = _enumerator.Current;

                var nextXform = _entityManager.GetComponent<TransformComponent>(nextGrid.Owner);
                if (nextXform.MapID != _mapId)
                {
                    continue;
                }

                var invMatrix3 = nextXform.InvWorldMatrix;
                var localAABB = invMatrix3.TransformBox(_worldAABB);

                if (!localAABB.Intersects(nextGrid.LocalAABB)) continue;

                var intersects = false;

                if (_entityManager.HasComponent<PhysicsComponent>(nextGrid.Owner))
                {
                    var enumerator = nextGrid.GetLocalMapChunks(localAABB);

                    if (!_approx)
                    {
                        var (worldPos, worldRot) = nextXform.GetWorldPositionRotation();

                        var transform = new Transform(worldPos, worldRot);

                        while (!intersects && enumerator.MoveNext(out var chunk))
                        {
                            foreach (var fixture in chunk.Fixtures)
                            {
                                for (var i = 0; i < fixture.Shape.ChildCount; i++)
                                {
                                    if (!fixture.Shape.ComputeAABB(transform, i).Intersects(_worldAABB)) continue;

                                    intersects = true;
                                    break;
                                }

                                if (intersects) break;
                            }
                        }
                    }
                    else
                    {
                        intersects = enumerator.MoveNext(out _);
                    }
                }

                if (!intersects && nextGrid.ChunkCount == 0 && !_worldAABB.Contains(nextXform.WorldPosition))
                {
                    continue;
                }

                grid = nextGrid;
                return true;
            }
        }

        public void Dispose()
        {
            _enumerator.Dispose();
        }
    }
}
