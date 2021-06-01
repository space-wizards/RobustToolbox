using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using Robust.Server;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Collision.Shapes;

namespace Robust.Benchmarks.Physics
{
    public class ManifoldBenchmark
    {
        private PolygonShape _polyA = new(0.01f);
        private PolygonShape _polyB = new(0.01f);

        private PhysShapeAabb _aabbA = new(0.01f) {LocalBounds = new Box2(-0.5f, 0.5f)};
        private PhysShapeAabb _aabbB = new(0.01f) {LocalBounds = new Box2(-0.5f, 0.5f)};

        private PhysShapeCircle _circleA = new() {Radius = 0.5f};

        private Transform _transformA = new Transform(Vector2.Zero, 0.0f);
        private Transform _transformB = new Transform(Vector2.Zero + 0.5f, 0.0f);
        private Transform _transformC = new Transform(Vector2.Zero + 10.0f, 0.0f);

        private ICollisionManager _collisionManager;

        public ManifoldBenchmark()
        {
            IoCManager.InitThread();
            ServerIoC.RegisterIoC();
            IoCManager.BuildGraph();

            var assemblies = new[]
            {
                AppDomain.CurrentDomain.GetAssemblyByName("Robust.Shared"),
                AppDomain.CurrentDomain.GetAssemblyByName("Robust.Server"),
                AppDomain.CurrentDomain.GetAssemblyByName("Robust.Benchmarks")
            };

            foreach (var assembly in assemblies)
            {
                IoCManager.Resolve<IConfigurationManagerInternal>().LoadCVarsFromAssembly(assembly);
            }

            _collisionManager = IoCManager.Resolve<ICollisionManager>();

            _polyA.SetAsBox(1.0f, 1.0f);
            _polyB.SetAsBox(1.0f, 1.0f);
        }

        [Benchmark]
        public void PolygonPolygonManifold()
        {
            var manifold = new Manifold();
            _collisionManager.CollidePolygons(ref manifold, _polyA, _transformA, _polyB, _transformB);
        }

        [Benchmark]
        public void PolygonPolygonNoCollide()
        {
            var manifold = new Manifold();
            _collisionManager.CollidePolygons(ref manifold, _polyA, _transformA, _polyB, _transformC);
        }

        [Benchmark]
        public void AABBAABBManifold()
        {
            var manifold = new Manifold();
            _collisionManager.CollideAabbs(ref manifold, _aabbA, _transformA, _aabbB, _transformB);
        }

        [Benchmark]
        public void AABBAABBNoCollide()
        {
            var manifold = new Manifold();
            _collisionManager.CollideAabbs(ref manifold, _aabbA, _transformA, _aabbB, _transformC);
        }

        [Benchmark]
        public void PolygonCircleManifold()
        {
            var manifold = new Manifold();
            _collisionManager.CollidePolygonAndCircle(ref manifold, _polyA, _transformA, _circleA, _transformB);
        }

        [Benchmark]
        public void PolygonCircleNoCollide()
        {
            var manifold = new Manifold();
            _collisionManager.CollidePolygonAndCircle(ref manifold, _polyA, _transformA, _circleA, _transformC);
        }

        [Benchmark]
        public void AABBCircleManifold()
        {
            var manifold = new Manifold();
            _collisionManager.CollideAabbAndCircle(ref manifold, _aabbA, _transformA, _circleA, _transformB);
        }

        [Benchmark]
        public void AABBCircleNoCollide()
        {
            var manifold = new Manifold();
            _collisionManager.CollideAabbAndCircle(ref manifold, _aabbA, _transformA, _circleA, _transformC);
        }
    }
}
