using NUnit.Framework;
using Robust.Shared.Collections;

namespace Robust.UnitTesting.Shared.Collections;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture, TestOf(typeof(RingBufferList<>))]
public sealed class RingBufferListTest
{
    [Test]
    public void TestBasicAdd()
    {
        var list = new RingBufferList<int>();
        list.Add(1);
        list.Add(2);
        list.Add(3);

        Assert.That(list, NUnit.Framework.Is.EquivalentTo(new[] {1, 2, 3}));
    }

    [Test]
    public void TestBasicAddAfterWrap()
    {
        var list = new RingBufferList<int>(6);
        list.Add(1);
        list.Add(2);
        list.Add(3);
        list.RemoveAt(0);
        list.Add(4);
        list.Add(5);
        list.Add(6);

        Assert.Multiple(() =>
        {
            // Ensure wrapping properly happened and we didn't expand.
            // (one slot is wasted by nature of implementation)
            Assert.That(list.Capacity, NUnit.Framework.Is.EqualTo(6));
            Assert.That(list, NUnit.Framework.Is.EquivalentTo(new[] { 2, 3, 4, 5, 6 }));
        });
    }

    [Test]
    public void TestMiddleRemoveAtScenario1()
    {
        var list = new RingBufferList<int>(6);
        list.Add(-1);
        list.Add(-1);
        list.Add(-1);
        list.Add(-1);
        list.Add(1);
        list.RemoveAt(0);
        list.RemoveAt(0);
        list.RemoveAt(0);
        list.RemoveAt(0);
        list.Add(2);
        list.Add(3);
        list.Add(4);
        list.Add(5);
        list.Remove(4);

        Assert.That(list, NUnit.Framework.Is.EquivalentTo(new[] {1, 2, 3, 5}));
    }

    [Test]
    public void TestMiddleRemoveAtScenario2()
    {
        var list = new RingBufferList<int>(6);
        list.Add(-1);
        list.Add(-1);
        list.Add(1);
        list.RemoveAt(0);
        list.RemoveAt(0);
        list.Add(2);
        list.Add(3);
        list.Add(4);
        list.Add(5);
        list.Remove(3);

        Assert.That(list, NUnit.Framework.Is.EquivalentTo(new[] {1, 2, 4, 5}));
    }

    [Test]
    public void TestMiddleRemoveAtScenario3()
    {
        var list = new RingBufferList<int>(6);
        list.Add(1);
        list.Add(2);
        list.Add(3);
        list.Add(4);
        list.Add(5);
        list.Remove(4);

        Assert.That(list, NUnit.Framework.Is.EquivalentTo(new[] {1, 2, 3, 5}));
    }
}
