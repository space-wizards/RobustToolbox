using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.Collections;

namespace Robust.UnitTesting.Shared.Collections;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture, TestOf(typeof(OverflowQueue<>))]
public sealed class OverflowDictionary_Test
{
    private static IEnumerable<(int size, int iterations)> TestParams => new[]
    {
        (10, 17),
        (10, 4)
    };

    [Test]
    public void ValueDisposerTest()
    {
        var disposedCalled = 0;
        var dict = new OverflowDictionary<int, int>(1, (_) => disposedCalled++);
        dict.Add(0,0);
        dict.Add(1,0);
        Assert.That(dict.ContainsKey(0), Is.False);
        Assert.That(dict.ContainsKey(1));
        Assert.That(disposedCalled, Is.EqualTo(1));
        Assert.That(dict.Count, Is.EqualTo(1));
    }

    [Test]
    public void TestQueue([ValueSource(nameof(TestParams))] (int size, int iterations) test)
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            var _ = new OverflowDictionary<int, int>(0);
        });

        var dict = new OverflowDictionary<int, int>(test.size);

        for (int i = 0; i < test.iterations; i++)
        {
            dict.Add(i, i+1);
        }

        Assert.That(dict.ContainsKey(test.iterations-1));
        Assert.Throws<InvalidOperationException>(() => dict.Add(test.iterations - 1, 0));

        var overlap = Math.Max(test.iterations - test.size, 0);
        for (int i = overlap; i < test.iterations; i++)
        {
            Assert.That(dict.TryGetValue(i, out var val));
            Assert.That(val, Is.EqualTo(i+1));
        }

        if(overlap > 0)
        {
            Assert.That(dict.ContainsKey(0), Is.False);
            Assert.That(dict.Count, Is.EqualTo(test.size));
        }
        else
        {
            Assert.That(dict.Count, Is.EqualTo(test.iterations));
        }

        dict.Clear();
        Assert.That(dict.Count, Is.EqualTo(0));

        //assert that the clear didnt mess up our internal ordering & array. just to make sure we didnt forget to reset something
        dict.Add(0, 1);
        Assert.That(dict.TryGetValue(0, out var value));
        Assert.That(value, Is.EqualTo(1));

        Assert.Throws<NotImplementedException>(() => dict.Remove(0));
    }

}
