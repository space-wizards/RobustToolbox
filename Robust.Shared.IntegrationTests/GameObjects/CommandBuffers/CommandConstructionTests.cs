using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.CommandBuffers;

namespace Robust.UnitTesting.Shared.GameObjects.CommandBuffers;

public sealed class CommandConstructionTests
{
    [Test]
    public void ConstructQueuedActionT()
    {
        CommandBufferEntry.QueuedActionT(ctx =>
            {
                Assert.That(ctx, NUnit.Framework.Is.Not.Null);
                Assert.Pass();
            },
            this,
            out var entry);

        entry.InvokeQueuedActionT();

        Assert.Fail("Invocation did nothing?");
    }

    [Test]
    public void ConstructQueuedActionTEnt()
    {
        CommandBufferEntry.QueuedActionTEnt(static (ctx, ent) =>
            {
                Assert.That(ctx, NUnit.Framework.Is.Not.Null);
                Assert.That(ent, NUnit.Framework.Is.EqualTo(EntityUid.FirstUid));
                Assert.Pass();
            },
            this,
            EntityUid.FirstUid,
            out var entry);

        entry.InvokeQueuedActionTEnt();

        Assert.Fail("Invocation did nothing?");
    }
}
