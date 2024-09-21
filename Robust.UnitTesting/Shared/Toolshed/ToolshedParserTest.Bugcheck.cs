using System.Threading.Tasks;
using NUnit.Framework;
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
        await Server.WaitAssertion(() => AssertResult("val Quantity 50%", new Quantity(null, 0.5f)));
    }

    // Toolshed outputting the wrong error kind here, it should not be an unknown command error.
    [Test, TestOf(typeof(ValueRef<>))]
    public async Task Bug_ValueRefUnknownCommandError_08_22_2023()
    {
        await Server.WaitAssertion(() =>
        {
            ParseError<InvalidColor>("val Color 180deg");
        });
    }
}
