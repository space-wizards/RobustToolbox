using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Robust.UnitTesting.Shared.Toolshed;

[TestFixture]
public sealed class ArithmeticTest : ToolshedTest
{
    [Test]
    public async Task OrderOfOperations()
    {
        await Server.WaitAssertion(() =>
        {
            // Toolshed always parses left-to-right with no precedence.
            Assert.That(InvokeCommand<float>("f 1 / 3"), Is.EqualTo(1.0f / 3.0f));
            Assert.That(InvokeCommand<float>("f 1 + 1 / 3"), Is.EqualTo((1.0f + 1.0f) / 3.0f));
            Assert.That(InvokeCommand<float>("f 2 + 2 pow 3"), Is.EqualTo(float.Pow(2.0f + 2.0f, 3.0f)));
        });
    }
    [Test]
    public async Task EveryNumericTypeOperateable()
    {
        await Server.WaitAssertion(() =>
        {
            InvokeCommand<byte, byte>("+ 1", 1);
            InvokeCommand<sbyte, sbyte>("+ 1", 1);
            InvokeCommand<short, short>("+ 1", 1);
            InvokeCommand<ushort, ushort>("+ 1", 1);
            InvokeCommand<int, int>("+ 1", 1);
            InvokeCommand<uint, uint>("+ 1", 1);
            InvokeCommand<long, long>("+ 1", 1);
            InvokeCommand<ulong, ulong>("+ 1", 1);
            InvokeCommand<nint, nint>("+ 1", 1);
            InvokeCommand<nuint, nuint>("+ 1", 1);
            InvokeCommand<float, float>("+ 1", 1);
            InvokeCommand<double, double>("+ 1", 1);
            InvokeCommand<BigInteger, BigInteger>("+ 1", 1);
            InvokeCommand<decimal, decimal>("+ 1", 1);
            InvokeCommand<Half, Half>("+ 1", Half.CreateTruncating(1)); // Can't use a constant for this one.
        });
    }

    [Test]
    public async Task NoOverflowException()
    {
        await Server.WaitAssertion(() =>
        {
            InvokeCommand<byte, byte>("+ 1", byte.MaxValue);
        });
    }

    [Test]
    public async Task Iterations()
    {
        await Server.WaitAssertion(() =>
        {
            var list = new List<float>();
            for (var i = 0; i < 100; i++)
            {
                list.Add(i + 1);
            }

            Assert.That(list, Is.EquivalentTo(InvokeCommand<IEnumerable<float>>("f 0 iterate { + 1 } 100")));

        });
    }
}
