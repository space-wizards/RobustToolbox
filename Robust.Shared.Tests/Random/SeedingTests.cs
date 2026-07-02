using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;

namespace Robust.Shared.Tests.Random;

[TestOf(typeof(IRobustRandom))]
public sealed class SeedingTests
{
    private struct MyContext(NetEntity ent1, int count)
    {
        public NetEntity Ent1 = ent1;
        public int Count = count;
    }

    [Test]
    [Description("Ensures two randomizers made with CreateSeeded output the same values at the same time, i.e. seeding is deterministic.")]
    public void SeedSimple()
    {
        const int seed = 17;

        var rng1 = IRobustRandom.CreateSeeded(seed);
        var rng2 = IRobustRandom.CreateSeeded(seed);
        using (Assert.EnterMultipleScope())
        {
            // We know what this seed outputs. It should always, always do the same thing.
            // If we change the underlying implementation, update this test.
            Assert.That(rng1.Next(1, 6), Is.EqualTo(4));
            Assert.That(rng2.Next(1, 6), Is.EqualTo(4));
        }

        // Try for a bit, no flukes.
        for (var i = 0; i < 128; i++)
        {
            Assert.That(rng1.Next(), Is.EqualTo(rng2.Next()), $"Failed at step {i}, randomizers diverged.");
        }
    }

    [Test]
    [Description("Ensures that creating seeds from a context is deterministic, testing two randomizers and asserting they output the same sequence.")]
    public void SeedWithContext()
    {
        var myContext = new MyContext(NetEntity.First, 42);

        var (rng1, seed1) = IRobustRandom.CreateSeededFromHashable(myContext);
        // Cheekily test the other method. They should be the same here.
        var (rng2, seed2) = IRobustRandom.CreateSeededFromHashable([myContext]);

        Assert.That(seed1, Is.EqualTo(seed2));
        Assert.That(seed1, Is.EqualTo(9377390), "Seed changed between runs. Something is off!");

        // Try for a bit, no flukes.
        for (var i = 0; i < 128; i++)
        {
            Assert.That(rng1.Next(), Is.EqualTo(rng2.Next()), $"Failed at step {i}, randomizers diverged.");
        }
    }

    [Test]
    [Description("Ensures that creating seeds from another randomizer is deterministic, testing two randomizers and asserting they output the same sequence.")]
    public void SeededWith()
    {
        var origin = IRobustRandom.CreateSeeded(17);

        var (descendant, seed) = IRobustRandom.CreateSeededWith(origin);

        Assert.That(IRobustRandom.CreateSeeded(17).Next(), Is.EqualTo(seed));

        // An important property of CreateSeededWith is it does *not* copy the seed of its parent rng.
        // So we ensure they've diverged already.
        Assert.That(descendant.Next(), Is.Not.EqualTo(origin.Next()));
    }
}
