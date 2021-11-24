using BenchmarkDotNet.Attributes;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Robust.Benchmarks.Transform
{
    public class TransformWorldBenchmark
    {
        // Most realistic scenario: entity -> grid -> map
        private TransformComponent _xform1 = default!;
        private TransformComponent _parent1 = default!;
        private TransformComponent _parent2 = default!;

        [GlobalSetup]
        public void Setup()
        {
            var entManager = new EntityManager();
            var ent1 = new Entity(entManager, new EntityUid(1));
            var ent2 = new Entity(entManager, new EntityUid(2));
            var ent3 = new Entity(entManager, new EntityUid(3));

            _xform1 = new TransformComponent
            {
                Owner = ent1,
            };
            _parent1 = new TransformComponent()
            {
                Owner = ent2
            };
            _parent2 = new TransformComponent()
            {
                Owner = ent3
            };

            _xform1.Parent = _parent1;
            _parent1.Parent = _parent2;
        }

        [Benchmark]
        public void BenchSeparate()
        {
            var (a, b) = _xform1.GetWorldPositionRotation();
        }

        [Benchmark]
        public void BenchTogether()
        {
            var (a, b) = (_xform1.WorldPosition, _xform1.WorldRotation);
        }
    }
}
