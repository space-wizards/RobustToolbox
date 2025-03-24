using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Robust.UnitTesting.Shared.GameState;

public sealed partial class VisibilityTest : RobustIntegrationTest
{
    /// <summary>
    /// This tests checks that entity visibility masks are recursively applied to children.
    /// </summary>
    [Test]
    public async Task UnknownEntityTest()
    {
        var server = StartServer();

        var xforms = server.System<SharedTransformSystem>();
        var vis = server.System<VisibilitySystem>();

        const int RequiredMask = 1;
        // All entities need to have this mask set ... which defeat the whole point of that bit?

        // Spawn a stack of entities
        int N = 6;
        var ents = new EntityUid[N];
        var metaComp = new MetaDataComponent[N];
        var visComp = new VisibilityComponent[N];
        await server.WaitPost(() =>
        {
            for (int i = 0; i < N; i++)
            {
                var ent = server.EntMan.Spawn();

                ents[i] = ent;
                metaComp[i] = server.EntMan.GetComponent<MetaDataComponent>(ent);
                visComp[i] = server.EntMan.AddComponent<VisibilityComponent>(ent);

                vis.AddLayer((ent, visComp[i]), (ushort)(1 << i));
                if (i > 0)
                    xforms.SetParent(ent, ents[i - 1]);
            }
        });

        // Each entity's visibility mask should include the parent's mask
        var mask = RequiredMask;
        for (int i = 0; i < N; i++)
        {
            mask |= 1 << i;
            var meta = metaComp[i];
            Assert.That(meta.VisibilityMask, Is.EqualTo(mask));
        }

        // Adding a layer to the root entity's mask will apply it to all children
        var extraMask = 1 << (N + 1);
        mask = RequiredMask | extraMask;
        vis.AddLayer((ents[0], visComp[0]), (ushort)extraMask);
        for (int i = 0; i < N; i++)
        {
            mask |= 1 << i;
            var meta = metaComp[i];
            Assert.That(meta.VisibilityMask, Is.EqualTo(mask));
        }

        // Removing the removes it from all children.
        vis.RemoveLayer((ents[0], visComp[0]), (ushort)extraMask);
        mask = RequiredMask;
        for (int i = 0; i < N; i++)
        {
            mask |= 1 << i;
            var meta = metaComp[i];
            Assert.That(meta.VisibilityMask, Is.EqualTo(mask));
        }

        // Detaching an entity from the stack updates it, and it's children's mask
        var split = N / 2;
        await server.WaitPost(() => xforms.SetParent(ents[split], EntityUid.Invalid));

        mask = RequiredMask;
        for (int i = 0; i < split; i++)
        {
            mask |= 1 << i;
            var meta = metaComp[i];
            Assert.That(meta.VisibilityMask, Is.EqualTo(mask));
        }

        mask = RequiredMask;
        for (int i = split; i < N; i++)
        {
            mask |= 1 << i;
            var meta = metaComp[i];
            Assert.That(meta.VisibilityMask, Is.EqualTo(mask));
        }

        // Re-attaching the entity also updates the masks.
        await server.WaitPost(() => xforms.SetParent(ents[split], ents[split - 1]));
        mask = RequiredMask;
        for (int i = 0; i < N; i++)
        {
            mask |= 1 << i;
            var meta = metaComp[i];
            Assert.That(meta.VisibilityMask, Is.EqualTo(mask));
        }

        // Setting a mask on a child does not propagate upwards, only downwards
        vis.AddLayer((ents[split], visComp[split]), (ushort)extraMask);
        mask = RequiredMask;
        for (int i = 0; i < split; i++)
        {
            mask |= 1 << i;
            var meta = metaComp[i];
            Assert.That(meta.VisibilityMask, Is.EqualTo(mask));
        }

        mask |= extraMask;
        for (int i = split; i < N; i++)
        {
            mask |= 1 << i;
            var meta = metaComp[i];
            Assert.That(meta.VisibilityMask, Is.EqualTo(mask));
        }
    }
}
