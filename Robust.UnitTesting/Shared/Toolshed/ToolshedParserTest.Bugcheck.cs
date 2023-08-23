using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Toolshed.TypeParsers.Math;

namespace Robust.UnitTesting.Shared.Toolshed;

// Find a silly little bug or a goof?
// Add a test here to make sure it doesn't come back.
public sealed partial class ToolshedParserTest
{
    // Memorializing the fact I never fixed this shit in BQL reee
    [Test, TestOf(typeof(Quantity))]
    public async Task Bug_QuantityPercentage_BeforeTime()
    {
        await Server.WaitAssertion(() =>
        {
            ParseCommand("val Quantity 50%");
        });
    }

    // Weird parsing issue around overly deep nesting.
    [Test, TestOf(typeof(Block))]
    public async Task Bug_DeepNest_08_21_2023()
    {
        await Server.WaitAssertion(() =>
        {
            ParseCommand("f 100 iota map { iota sum emplace { f 2 pow $value } }");
        });
    }

    // Toolshed outputting the wrong error kind here, it should not be an unknown command error.
    [Test, TestOf(typeof(ValueRef<>))]
    public async Task Bug_ValueRefUnknownCommandError_08_22_2023()
    {
        await Server.WaitAssertion(() =>
        {
            ExpectError<InvalidColor>();
            ParseCommand("val Color 180deg");
        });
    }

    // Terminator parsing would try to eat an entire word instead of only the terminator.
    // This was fixed with the introduction of TryMatch.
    [Test, TestOf(nameof(ParserContext.EatTerminator)), TestOf(nameof(ParserContext.TryMatch))]
    public async Task Bug_TerminatorSpacing_08_23_2023()
    {
        await Server.WaitAssertion(() =>
        {
            ParseCommand("f 100 iota map {iota sum emplace {f 2 pow $value}}");
        });
    }
}
