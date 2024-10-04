using System;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Toolshed;

public sealed partial class ToolshedParserTest
{
    [Test]
    public async Task AllCoreTypesParseable()
    {
        await Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                // Integer types.
                AssertParseable<byte>();
                AssertParseable<sbyte>();
                AssertParseable<short>();
                AssertParseable<ushort>();
                AssertParseable<int>();
                AssertParseable<uint>();
                AssertParseable<long>();
                AssertParseable<ulong>();
                AssertParseable<nint>();
                AssertParseable<nuint>();
                AssertParseable<BigInteger>();
                AssertParseable<decimal>();
                AssertParseable<Half>();

                // Common
                AssertParseable<bool>();
                AssertParseable<string>();
                AssertParseable<EntityUid>();
                AssertParseable<ResPath>();
                AssertParseable<Type>();
                AssertParseable<TestEnum>();

                // maff
                AssertParseable<Vector2>();
                AssertParseable<Vector2i>();
                AssertParseable<Robust.Shared.Maths.Vector3>();
                AssertParseable<Robust.Shared.Maths.Vector4>();
                AssertParseable<Box2>();
                AssertParseable<Box2i>();
                AssertParseable<Angle>();
                AssertParseable<Color>();
                AssertParseable<Direction>();
                AssertParseable<DirectionFlag>();
                AssertParseable<UIBox2>();
                AssertParseable<UIBox2i>();

                // The tuples. *scream
                AssertParseable<ValueTuple<int, int>>();
                AssertParseable<ValueTuple<int, int, int>>();
                AssertParseable<ValueTuple<int, int, int, int>>();
                AssertParseable<ValueTuple<int, int, int, int, int>>();
                AssertParseable<ValueTuple<int, int, int, int, int, int>>();
                AssertParseable<ValueTuple<int, int, int, int, int, int, int>>();
                // this line literally breaks rider, uncomment when it doesn't.
                //AssertParseable<ValueTuple<int, int, int, int, int, int, int, ValueTuple>>();

                // Toolshed special constructs.
                AssertParseable<ValueRef<object>>();
                AssertParseable<CommandRun>();
                AssertParseable<CommandRun<object>>();
                AssertParseable<CommandRun<object, object>>();
                AssertParseable<Block>();
                AssertParseable<Block<object>>();
                AssertParseable<Block<object, object>>();
            });
        });
    }

    private enum TestEnum
    {
        A = 0,
        B = 1
    }
}
