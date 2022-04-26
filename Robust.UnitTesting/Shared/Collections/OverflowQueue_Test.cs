using System;
using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Collections;

namespace Robust.UnitTesting.Shared.Collections;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture, TestOf(typeof(OverflowQueue<>))]
public sealed class OverflowQueue_Test
{
    private static IEnumerable<(int size, int iterations)> TestParams => new[]
    {
        (10, 17),
        (10, 4)
    };

    [Test]
    public void TestQueue([ValueSource(nameof(TestParams))] (int size, int iterations) test)
    {
        var queue = new OverflowQueue<int>(test.size);

        Assert.False(queue.Contains(0));

        for (int i = 0; i < test.iterations; i++)
        {
            queue.Enqueue(i);
        }

        var overlap = Math.Max(test.iterations - test.size, 0);
        for (int i = overlap; i < test.iterations; i++)
        {
            Assert.That(queue.Contains(i));
        }

        Assert.That(queue.Contains(test.iterations-1));
        Assert.False(queue.Contains(-1));

        Assert.That(queue.Peek(), Is.EqualTo(overlap));
        var array = queue.ToArray();
        Assert.That(array.Length, Is.EqualTo(Math.Min(test.size, test.iterations)));
        for (var i = 0; i < array.Length; i++)
        {
            Assert.That(array[i], Is.EqualTo(i + overlap));
        }

        for (int i = overlap; i < test.iterations; i++)
        {
            Assert.That(queue.TryDequeue(out var item));
            Assert.That(item, Is.EqualTo(i));
        }

        Assert.Throws<InvalidOperationException>(() => queue.Dequeue());
        Assert.False(queue.TryDequeue(out _));
    }
}
