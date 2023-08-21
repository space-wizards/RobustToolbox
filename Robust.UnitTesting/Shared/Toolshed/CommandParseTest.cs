using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Toolshed;

[TestFixture]
public sealed class CommandRunTest : ToolshedTest
{
    [Test]
    public async Task SimpleCommandRun()
    {
        await Server.WaitAssertion(() =>
        {
            ParseCommand("entities");
            ParseCommand("entities select 1");
            ParseCommand("entities with MetaData select 1");

            ExpectError<OutOfInputError>();
            ParseCommand("entities with");

            ExpectError<NoImplementationError>();
            ParseCommand("player:list with MetaData");

            ExpectError<ExpressionOfWrongType>();
            ParseCommand("player:list", expectedType: typeof(IEnumerable<EntityUid>));

            ParseCommand("entities not with MetaData");
            ParseCommand("with MetaData select 2 any", inputType: typeof(List<EntityUid>));

            ParseCommand("entities not with MetaData => $myEntities");
            ParseCommand("=> $fooBar with MetaData", inputType: typeof(List<EntityUid>));
        });
    }

    [Test]
    public async Task EntityUidTypeParser()
    {
        await Server.WaitAssertion(() =>
        {
            ParseCommand("ent 1");
            ParseCommand("ent c1");

            ExpectError<InvalidEntityUid>();
            ParseCommand("ent foodigity");
        });
    }

    [Test]
    public async Task QuantityTypeParser()
    {
        await Server.WaitAssertion(() =>
        {
            ParseCommand("entities select 100");
            ParseCommand("entities select 50%");

            ExpectError<InvalidQuantity>();
            ParseCommand("entities select -1");

            ExpectError<InvalidQuantity>();
            ParseCommand("entities select 200%");

            ExpectError<InvalidQuantity>();
            ParseCommand("entities select hotdog");
        });
    }

    [Test]
    public async Task ComponentTypeParser()
    {
        await Server.WaitAssertion(() =>
        {
            ParseCommand("entities with MetaData");

            ExpectError<UnknownComponentError>();
            ParseCommand("entities with Foodiddy");

            ExpectError<UnknownComponentError>();
            ParseCommand("entities with MetaDataComponent");
        });
    }

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
            AssertParseable<Enum>();
            AssertParseable<TimeSpan>();
            AssertParseable<DateTime>();
            AssertParseable<Uri>();

            // maff
            AssertParseable<Vector2>();
            AssertParseable<Vector2i>();
            AssertParseable<Robust.Shared.Maths.Vector3>();
            AssertParseable<Robust.Shared.Maths.Vector4>();
            AssertParseable<Robust.Shared.Maths.Matrix22>();
            AssertParseable<Robust.Shared.Maths.Matrix33>();
            AssertParseable<Robust.Shared.Maths.Matrix3>();
            AssertParseable<Robust.Shared.Maths.Matrix4>();
            AssertParseable<Box2>();
            AssertParseable<Box2Rotated>();
            AssertParseable<Box2i>();
            AssertParseable<Angle>();
            AssertParseable<Circle>();
            AssertParseable<Color>();
            AssertParseable<Direction>();
            AssertParseable<DirectionFlag>();
            AssertParseable<UIBox2>();
            AssertParseable<UIBox2i>();
            AssertParseable<Thickness>();

            // The tuples. *scream
            AssertParseable<ValueTuple<object>>();
            AssertParseable<ValueTuple<object, object>>();
            AssertParseable<ValueTuple<object, object, object>>();
            AssertParseable<ValueTuple<object, object, object, object>>();
            AssertParseable<ValueTuple<object, object, object, object, object>>();
            AssertParseable<ValueTuple<object, object, object, object, object, object>>();
            AssertParseable<ValueTuple<object, object, object, object, object, object, object>>();
            AssertParseable<ValueTuple<object, object, object, object, object, object, object, ValueTuple>>();

            // Toolshed special constructs.
            AssertParseable<ValueRef<object>>();
            AssertParseable<ValueRef<object, object>>();
            AssertParseable<CommandRun>();
            AssertParseable<CommandRun<object>>();
            AssertParseable<CommandRun<object, object>>();
            AssertParseable<Block>();
            AssertParseable<Block<object>>();
            AssertParseable<Block<object, object>>();

            // The fallback.
            AssertParseable<FallbackTest>();
            });
        });
    }

    private sealed class FallbackTest : IParsable<FallbackTest>
    {
        public static FallbackTest Parse(string s, IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        public static bool TryParse(string? s, IFormatProvider? provider, out FallbackTest result)
        {
            throw new NotImplementedException();
        }
    }

    [Test]
    public async Task DeepNest()
    {
        await Server.WaitAssertion(() =>
        {
            ParseCommand("f 100 iota map { iota sum emplace { f 2 pow $value } }");
        });
    }
}
