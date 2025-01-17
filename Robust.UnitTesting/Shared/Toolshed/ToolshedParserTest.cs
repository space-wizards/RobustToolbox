using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Toolshed.TypeParsers.Math;

namespace Robust.UnitTesting.Shared.Toolshed;

[TestFixture]
public sealed partial class ToolshedParserTest : ToolshedTest
{
    [Test]
    public async Task SimpleCommandRun()
    {
        await Server.WaitAssertion(() =>
        {
            ParseCommand("entities");
            ParseCommand("entities select 1");
            ParseCommand("entities with MetaData select 1");

            ParseError<ExpectedArgumentError>("entities with");

            ParseError<NoImplementationError>("player:list with MetaData");

            ExpectError<WrongCommandReturn>();
            ParseCommand("player:list", expectedType: typeof(IEnumerable<EntityUid>));

            ParseCommand("entities not with MetaData");
            ParseCommand("with MetaData select 2 any", inputType: typeof(List<EntityUid>));

            ParseCommand("entities not with MetaData => $myEntities");
            ParseCommand("=> $fooBar with MetaData", inputType: typeof(List<EntityUid>));
        });
    }

    [Test]
    public async Task EntityTypeParser()
    {
        await Server.WaitAssertion(() =>
        {
            ParseCommand("ent 1");
            // Clientside entities are a myth.

            ParseError<InvalidEntity>("ent foodigity");
        });
    }

    [Test]
    public async Task QuantityTypeParser()
    {
        await Server.WaitAssertion(() =>
        {
            ParseCommand("entities select 100");
            ParseCommand("entities select 50%");
            ParseError<InvalidQuantity>("entities select -1");
            ParseError<InvalidQuantity>("entities select 200%");
            ParseError<InvalidQuantity>("entities select hotdog");
        });
    }

    [Test]
    public async Task ComponentTypeParser()
    {
        await Server.WaitAssertion(() =>
        {
            ParseCommand("entities with MetaData");
            ParseError<UnknownComponentError>("entities with Foodiddy");
            ParseError<UnknownComponentError>("entities with MetaDataComponent");
        });
    }

    [Test]
    public async Task ColorParseTest()
    {
        await Server.WaitAssertion(() =>
        {
            AssertResult("val Color red", Color.Red);
            AssertResult("val Color blue", Color.Blue);
            AssertResult("val Color green", Color.Green);
            AssertResult("val Color #89ABCD", Color.FromHex("#89ABCD"));
            AssertResult("val Color #89ABCDEF", Color.FromHex("#89ABCDEF"));
        });
    }

    [Test]
    public async Task AngleParseTest()
    {
        await Server.WaitAssertion(() =>
        {
            AssertResult("val Angle 3.14159", new Angle(3.14159f));
            AssertResult("val Angle 180deg", Angle.FromDegrees(180));
        });
    }

    [Test]
    public async Task Vector2ParseTest()
    {
        await Server.WaitAssertion(() =>
        {
            AssertResult("val Vector2 [1, 1]", new Vector2(1, 1));
            AssertResult("val Vector2 [-1, 1]", new Vector2(-1, 1));
            AssertResult("val Vector2 [ 1  , 1    ]", new Vector2(1, 1));
            AssertResult("val Vector2 [ -1, 1 ]", new Vector2(-1, 1));
            ParseError<ExpectedOpenBrace>("val Vector2 1, 1");
            ParseError<ExpectedCloseBrace>("val Vector2 [1, 1");
            ParseError<UnexpectedCloseBrace>("val Vector2 [1]");
        });
    }
}
