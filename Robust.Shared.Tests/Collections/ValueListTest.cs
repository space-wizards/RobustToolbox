using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Robust.Shared.Collections;

namespace Robust.Shared.Tests.Collections;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture, TestOf(typeof(ValueList<>))]
internal sealed class ValueListTest
{
    [Test]
    public void TryPopClearsRemovedReference()
    {
        var list = new ValueList<object>(1);
        var item = new object();
        list.Add(item);

        Assert.That(list.TryPop(out var popped), Is.True);
        Assert.That(popped, Is.SameAs(item));

        var itemsField = typeof(ValueList<object>).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance);
        var items = (object?[]?) itemsField!.GetValue(list);

        Assert.That(items, Is.Not.Null);
        Assert.That(items![0], Is.Null);
    }

    [Test]
    public void IListMethodsMatchList()
    {
        IList<int> expected = new List<int>();
        IList<int> actual = new ValueList<int>();

        AssertListEqual(expected, actual);
        Assert.That(actual.IsReadOnly, Is.EqualTo(expected.IsReadOnly));

        expected.Add(1);
        actual.Add(1);
        AssertListEqual(expected, actual);

        expected.Add(3);
        actual.Add(3);
        AssertListEqual(expected, actual);

        expected.Insert(1, 2);
        actual.Insert(1, 2);
        AssertListEqual(expected, actual);

        expected[2] = 4;
        actual[2] = 4;
        AssertListEqual(expected, actual);

        Assert.That(actual.IndexOf(2), Is.EqualTo(expected.IndexOf(2)));
        Assert.That(actual.IndexOf(99), Is.EqualTo(expected.IndexOf(99)));
        Assert.That(actual.Contains(4), Is.EqualTo(expected.Contains(4)));
        Assert.That(actual.Contains(99), Is.EqualTo(expected.Contains(99)));

        var expectedCopy = new int[5];
        var actualCopy = new int[5];
        expected.CopyTo(expectedCopy, 1);
        actual.CopyTo(actualCopy, 1);
        Assert.That(actualCopy, Is.EqualTo(expectedCopy));

        Assert.That(actual.Remove(2), Is.EqualTo(expected.Remove(2)));
        AssertListEqual(expected, actual);

        expected.RemoveAt(1);
        actual.RemoveAt(1);
        AssertListEqual(expected, actual);

        expected.Clear();
        actual.Clear();
        AssertListEqual(expected, actual);
    }

    [Test]
    public void IListInsertGrowsDefaultList()
    {
        IList<int> expected = new List<int>();
        IList<int> actual = new ValueList<int>();

        expected.Insert(0, 10);
        actual.Insert(0, 10);

        AssertListEqual(expected, actual);
    }

    [Test]
    public void IListCopyToExceptionsMatchList()
    {
        IList<int> expected = new List<int> { 1, 2, 3 };
        IList<int> actual = new ValueList<int>(expected);

        AssertSameException(() => expected.CopyTo(null!, 0), () => actual.CopyTo(null!, 0));
        AssertSameException(() => expected.CopyTo(new int[3], -1), () => actual.CopyTo(new int[3], -1));
        AssertSameException(() => expected.CopyTo(new int[3], 1), () => actual.CopyTo(new int[3], 1));
        AssertSameException(() => expected.CopyTo(new int[2], 0), () => actual.CopyTo(new int[2], 0));
    }

    [Test]
    public void IListIndexExceptionsMatchList()
    {
        IList<int> expected = new List<int> { 1, 2, 3 };
        IList<int> actual = new ValueList<int>(expected);

        AssertSameException(() => _ = expected[-1], () => _ = actual[-1]);
        AssertSameException(() => _ = expected[3], () => _ = actual[3]);
        AssertSameException(() => expected[-1] = 0, () => actual[-1] = 0);
        AssertSameException(() => expected[3] = 0, () => actual[3] = 0);
    }

    [Test]
    public void IListMutationExceptionsMatchList()
    {
        IList<int> expected = new List<int> { 1, 2, 3 };
        IList<int> actual = new ValueList<int>(expected);

        AssertSameException(() => expected.Insert(-1, 0), () => actual.Insert(-1, 0));
        AssertSameException(() => expected.Insert(4, 0), () => actual.Insert(4, 0));
        AssertSameException(() => expected.RemoveAt(-1), () => actual.RemoveAt(-1));
        AssertSameException(() => expected.RemoveAt(3), () => actual.RemoveAt(3));
    }

    private static void AssertListEqual<T>(IList<T> expected, IList<T> actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count));
            Assert.That(actual, Is.EqualTo(expected));
        });
    }

    private static void AssertSameException(TestDelegate expected, TestDelegate actual)
    {
        var expectedException = Assert.Throws(Is.InstanceOf<Exception>(), expected);
        var actualException = Assert.Throws(Is.InstanceOf<Exception>(), actual);

        Assert.That(actualException, Is.TypeOf(expectedException!.GetType()));
    }
}
