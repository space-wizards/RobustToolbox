using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.UnitTesting;

namespace Robust.Shared.IntegrationTests.GameObjects;

public sealed partial class EntityQueryTests : RobustIntegrationTest
{
    [Test]
    public async Task TestEntityQueryForEach()
    {
        var server = StartServer();

        await server.WaitIdleAsync();
        await server.WaitPost(() =>
        {
            var sEntManager = server.ResolveDependency<IEntityManager>();
            for (var i = 0; i < 5; i++)
            {
                var ent = sEntManager.Spawn(null, MapCoordinates.Nullspace);
                sEntManager.EnsureComponent<EntityQueryTestsAComponent>(ent);
            }

            for (var i = 0; i < 10; i++)
            {
                var ent = sEntManager.Spawn(null, MapCoordinates.Nullspace);
                sEntManager.EnsureComponent<EntityQueryTestsBComponent>(ent);
            }
        });

        await server.WaitAssertion(() =>
        {
            var sEntManager = server.ResolveDependency<IEntityManager>();
            var a = new List<Entity<EntityQueryTestsAComponent>>();
            foreach (var ent in sEntManager.EntityQueryEnumerator<EntityQueryTestsAComponent>())
            {
                Assert.That(a, Does.Not.Contain(ent));
                a.Add(ent);
            }

            Assert.That(a, Has.Count.EqualTo(5));

            var b = new List<Entity<EntityQueryTestsBComponent>>();
            foreach (var ent in sEntManager.EntityQueryEnumerator<EntityQueryTestsBComponent>())
            {
                Assert.That(b, Does.Not.Contain(ent));
                b.Add(ent);
            }

            Assert.That(b, Has.Count.EqualTo(10));

            var c = new List<Entity<EntityQueryTestsAComponent, EntityQueryTestsBComponent>>();
            foreach (var ent in sEntManager.EntityQueryEnumerator<EntityQueryTestsAComponent, EntityQueryTestsBComponent>())
            {
                c.Add(ent);
            }

            Assert.That(c, Is.Empty);
        });
    }

    [RegisterComponent]
    public sealed partial class EntityQueryTestsAComponent : Component;

    [RegisterComponent]
    public sealed partial class EntityQueryTestsBComponent : Component;
}
