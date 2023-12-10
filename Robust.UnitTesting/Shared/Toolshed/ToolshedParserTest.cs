using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
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
    public async Task EntityTypeParser()
    {
        await Server.WaitAssertion(() =>
        {
            ParseCommand("ent 1");
            // Clientside entities are a myth.

            ExpectError<InvalidEntity>();
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
    public async Task ColorParseTest()
    {
        await Server.WaitAssertion(() =>
        {
            ParseCommand("val Color red");
            ParseCommand("val Color blue");
            ParseCommand("val Color green");
            ParseCommand("val Color #FF0000");
            ParseCommand("val Color #00FF00");
            ParseCommand("val Color #0000FF");
        });
    }

    [Test]
    public async Task AngleParseTest()
    {
        await Server.WaitAssertion(() =>
        {
            ParseCommand("val Angle 3.14159");
            ParseCommand("val Angle 180deg");
        });
    }

    [Test]
    public async Task Vector2ParseTest()
    {
        await Server.WaitAssertion(() =>
        {
            ParseCommand("val Vector2 [1, 1]");
            ParseCommand("val Vector2 [-1, 1]");
            ParseCommand("val Vector2 [ 1  , 1    ]");
            ParseCommand("val Vector2 [ -1, 1 ]");

            ExpectError<ExpectedOpenBrace>();
            ParseCommand("val Vector2 1, 1");

            ExpectError<ExpectedCloseBrace>();
            ParseCommand("val Vector2 [1, 1");

            ExpectError<UnexpectedCloseBrace>();
            ParseCommand("val Vector2 [1]");
        });
    }
}
