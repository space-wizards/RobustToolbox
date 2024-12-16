using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Commands.Generic;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Toolshed.TypeParsers.Math;

namespace Robust.UnitTesting.Shared.Toolshed;

/// <summary>
/// Collection of miscellaneous toolshed command tests.
/// Several of these were just added ad hoc as bugs arose.
/// </summary>
public sealed class ToolshedTests : ToolshedTest
{
    // TODO Robust.UnitTesting
    // split these into separate [TestCase]s when we have pooling.
    // Or some other way to avoid starting a server per command.
    [Test]
    public async Task TestMiscCommands()
    {
        await Server.WaitAssertion(() =>
        {
            AssertResult("i 5 iota reduce { max $value }", 5);
            AssertResult("i 5 iota sum", 15);
            AssertResult("i 5 iota map { + 1 }", new[] {2, 3, 4, 5, 6});
            AssertResult("f 5 iota map { iota sum emplace { f 2 pow $value } }", new[] {2.0f, 8.0f, 64.0f, 1024.0f, 32768.0f});
            AssertResult("f 5 iota map {iota sum emplace {f 2 pow $value}}", new[] {2.0f, 8.0f, 64.0f, 1024.0f, 32768.0f});
            AssertResult("i 0 map { + 1 }", new [] { 1 });
            AssertResult("i 1 to 1", new [] { 1 });
            AssertResult("f 1 to 1", new [] { 1 });
            AssertResult("i -2 to 2 map { + 1 }", new [] { -1, 0, 1, 2, 3 });
            AssertResult("f -2 to 2 map { + 1 }", new [] { -1, 0, 1, 2, 3 });
            AssertResult("f 1 to 1", new [] { 1 });
            AssertResult("i 2 + 2", 4);
            AssertResult("i 2 + { i 2 }", 4);
            AssertResult("i 2 + 2 * 2", 8);
            AssertResult("i 2 + { i 2 * 2 }", 6);
            AssertResult("i 3 iota max { i 3 iota }", new[] {1, 2, 3 });
            AssertResult("i 1 iota iterate { take 1 } 3", new[] { new[]{1}, [1], [1] });
            AssertResult("i 3 iota iterate { take 2 } 3", new[] { new[]{1, 2}, [1, 2], [1, 2] });

            ParseError<OutOfInputError>("");
            ParseError<OutOfInputError>(" ");
            ParseError<NotValidCommandError>("{");
            ParseError<NotValidCommandError>("}");
            ParseError<NotValidCommandError>("{}");
            ParseError<NotValidCommandError>(";");
            ParseError<EmptyCommandRun>("i 2 + { }");
            ParseError<MustBeVarOrBlock>("i 3 iota max 3");
            ParseError<WrongCommandReturn>("i 1 iota iterate { average } 2");

            // Meta-test: check that ParseError() fails if the parse actually succeeds without generating an error
            Assert.Throws<AssertionException>(() => ParseError<OutOfInputError>("i 2"));
            Assert.That(ExpectedErrors.Count, Is.EqualTo(1));
            ExpectedErrors.Clear();
        });
    }

    [Test]
    public async Task TestTypeArgs()
    {
        await Server.WaitAssertion(() =>
        {
            AssertResult("testtypearg int", "Int32");
            AssertResult("testmultitypearg float string -1", "Single, String, -1");

            ParseError<ExpectedTypeArgumentError>("testtypearg");
            ParseError<ExpectedTypeArgumentError>("testtypearg ");
            ParseError<UnknownType>("testtypearg invalidType");
            ParseError<ExpectedTypeArgumentError>("testmultitypearg int");
            ParseError<ExpectedTypeArgumentError>("testmultitypearg int ");
            ParseError<UnknownType>("testmultitypearg int invalidType");
            ParseError<ExpectedArgumentError>("testmultitypearg int float");
            ParseError<ExpectedArgumentError>("testmultitypearg int float ");
        });
    }

    [Test]
    public async Task TestCommandTerminator()
    {
        await Server.WaitAssertion(() =>
        {
            // Terminators can be used to chain together commands that output void
            AssertResult("testvoid", null);
            ParseError<EndOfCommandError>("testvoid testvoid");
            AssertResult("testvoid;testvoid", null);
            AssertResult("testvoid; testvoid", null);
            AssertResult("testvoid ; testvoid", null);
            AssertResult("testvoid ;; ; testvoid", null);

            // Terminators allow commands that output data to be chained with commands that take no inputs
            AssertResult("testint", 1);
            AssertResult("testint; testint", 1);
            AssertResult("testvoid; testint", 1);
            AssertResult("testint; testvoid", null);

            // Terminators can interrupt argument parsing
            AssertResult("testintstrarg 1 \"A\"", 1);
            AssertResult("testintstrarg 1 \"A\"; testvoid", null);
            ParseError<ExpectedArgumentError>("testintstrarg 1;");
            ParseError<ExpectedArgumentError>("testintstrarg 1; \"A\"");
            ParseError<ExpectedArgumentError>("testintstrarg 1; testvoid");
            ParseError<ExpectedArgumentError>("testintstrarg 1; \"A\"; testvoid");

            // Terminators can interrupt type-argument parsing
            AssertResult("testmultitypearg float string -1", "Single, String, -1");
            AssertResult("testmultitypearg float string -1; testvoid", null);
            ParseError<ExpectedTypeArgumentError>("testmultitypearg float;");
            ParseError<ExpectedTypeArgumentError>("testmultitypearg float; string -1");
            ParseError<ExpectedTypeArgumentError>("testmultitypearg float; testvoid");
            ParseError<ExpectedArgumentError>("testmultitypearg float string; testvoid");

            // Terminators don't actually discard the final output type if it is the end of the command.;
            AssertResult("testint;", 1);
            AssertResult("testint; testint;", 1);
            AssertResult("i 2 + { i 2; }", 4);
            AssertResult("i 2 + { i 2; ; } ;; ;", 4);
        });
    }

    [Test]
    public async Task TestVariables()
    {
        await Server.WaitAssertion(() =>
        {
            AssertResult("i 2 => $x", 2);
            AssertResult("val int $x", 2);
            AssertResult("var $x", 2);
            AssertResult("i 2 + $x", 4);
            AssertResult("i 2 + $x + 2 ", 6);
            AssertResult("i 2 + { i 2 * $x }", 6);
            AssertResult("i 5 iota emplace { val int $value }", new[] {1, 2, 3, 4, 5});
            AssertResult("i 5 iota emplace { var $value }", new[] {1, 2, 3, 4, 5});
            AssertResult("i 5 iota emplace { i 1 + $value }", new[] {2, 3, 4, 5, 6});
            AssertResult("i 5 iota reduce { + $value }", 15);
        });
    }

    [Test, TestOf(typeof(EmplaceCommand))]
    public async Task TestEmplace()
    {
        await Server.WaitAssertion(() =>
        {

            var ent = Server.EntMan.Spawn();
            Server.System<MetaDataSystem>().SetEntityName(ent, "Foo");

            AssertResult($"ent e{ent.Id} emplace {{ val EntityUid $value }}", ent);
            AssertResult($"ent e{ent.Id} emplace {{ var $value }}", ent);
            AssertResult($"ent e{ent.Id} emplace {{ f 2 + $wx }}", 2.0f);
            AssertResult($"ent e{ent.Id} emplace {{ var $name }}", "Foo");
            AssertResult($"player:list emplace {{ var $value }} ", Array.Empty<ICommonSession>());
            AssertResult($"player:list emplace {{ var $ent }} ", Array.Empty<EntityUid>());

            AssertCompletionSingle($"ent e{ent.Id} emplace ", "{");
            AssertCompletionContains($"ent e{ent.Id} emplace {{ ", "val", "var", "f");
            AssertCompletionContains($"ent e{ent.Id} emplace {{ val EntityUi", "EntityUi");
            AssertCompletionContains($"ent e{ent.Id} emplace {{ val EntityUid", "EntityUid");
            AssertCompletionContains($"ent e{ent.Id} emplace {{ val EntityUid $", "$value");
            AssertCompletionContains($"ent e{ent.Id} emplace {{ val EntityUid $val", "$value");
            AssertCompletionContains($"ent e{ent.Id} emplace {{ var ", "$value", "$wx", "$paused");
            AssertCompletionContains($"ent e{ent.Id} emplace {{ var $", "$value", "$wx", "$paused");
            AssertCompletionContains($"ent e{ent.Id} emplace {{ var $val", "$value");
            AssertCompletionSingle($"ent e{ent.Id} emplace {{ var $value ", "}");

            // Intentionally misspelled variable name
            AssertCompletionEmpty($"ent e{ent.Id} emplace {{ var $valuie ");

            AssertResult($"ent e{ent.Id} emplace {{ delete $value; i 5 }}", 5 );
            Assert.That(Server.EntMan.Deleted(ent));

            AssertCompletionContains("i 2 emplace { var ", "$value");
            AssertCompletionInvalid("i 2 emplace { var ", "wx");
            AssertCompletionContains("player:list emplace { var ", "$value", "$ent", "$paused");
            AssertCompletionInvalid("player:list emplace { var ", "wx");
            AssertCompletionContains("i 1 emplace { var $value empla", "emplace");
            AssertCompletionSingle("i 1 emplace { var $value emplace ", "{");
            AssertCompletionContains("player:list emplace { var $ent emplace { var $", "$value", "$wx", "$paused");

            ParseError<NotValidCommandError>("i 1 emplace {{");
        });
    }

    [Test]
    public async Task TestCompletions()
    {
        await Server.WaitAssertion(() =>
        {
            InvokeCommand($"i 1 => $x", out _);

            // Valid/complete commands ending in a whitespace don't generate completions.
            AssertCompletionEmpty($"i 1 ");
            AssertCompletionEmpty($"i 1 => $x ");
            AssertCompletionEmpty($"testvoid ");

            // Without a whitespace, they will still suggest the hint for the command that is currently being typed.
            AssertCompletionHint("i 1", "Int32");
            AssertCompletionSingle($"i 1 => $x", "$x");
            AssertCompletionContains($"testvoid", "testvoid");

            // If an error occurs while parsing something, but tha error is not at the end of the command, we should
            // not generate completions. I.e., we don't want to mislead people into thinking a command is valid and is
            // expecting additional arguments.
            AssertCompletionHint("i a", "Int32");
            AssertCompletionEmpty("i a ");
            AssertCompletionEmpty("i a 1");
            AssertCompletionSingle("i $", "$x");
            AssertCompletionEmpty("i $a ");
            AssertCompletionSingle("var $", "$x");
            AssertCompletionEmpty("var $a ");

            // Test variable completion
            AssertCompletionSingle($"i 1 + $", "$x");
            AssertCompletionSingle($"i 1 + $x", "$x");
            AssertCompletionEmpty($"i 1 + $x ");

            // Completion suggestions are restricted based on the piped type.
            AssertCompletionContains("", "i");
            AssertCompletionInvalid("", "testpipedint");
            AssertCompletionEmpty("i 5 ");
            AssertCompletionContains("i 5 t", "testpipedint");
            AssertCompletionInvalid("i 5 ", "i");
            AssertCompletionInvalid("i 5; t", "testpipedint");
            AssertCompletionEmpty("i 5; ");
            AssertCompletionContains("i 5; i", "i");

            // Check completions when typing out; var $x
            AssertCompletionContains($"va", "val", "var", "vars");
            AssertCompletionContains($"var", "var", "vars");
            AssertCompletionSingle($"var ", "$x");
            AssertCompletionSingle($"var $", "$x");
            AssertCompletionSingle($"var $x", "$x");
            AssertCompletionEmpty($"var $x ");

            // Check completions when typing out: testintstrarg 1 "a"
            AssertCompletionContains("testintstrarg", "testintstrarg");
            AssertCompletionHint("testintstrarg ", "Int32");
            AssertCompletionHint("testintstrarg 1", "Int32");
            AssertCompletionSingle("testintstrarg 1 ", "\"");
            AssertCompletionHint("testintstrarg 1 \"", "<string>");
            AssertCompletionHint("testintstrarg 1 \"a\"", "<string>");
            AssertCompletionEmpty("testintstrarg 1 \"a\" ");
            AssertCompletionHint("testintstrarg 1 \"a\" + ", "Int32");

            AssertCompletionContains("i 5 iota reduce { ma", "max");
            AssertCompletionContains("i 5 iota reduce { max $", "$x", "$value");

            AssertCompletionEmpty("notarealcommand ");
            AssertCompletionEmpty("i 2 + { notarealcommand ");
        });
    }

    [Test]
    public async Task TestCustomParser()
    {
        await Server.WaitAssertion(() =>
        {
            InvokeCommand("i -1 => $x", out _);

            // custom parsers still result in argument expectation erroprs
            ParseError<ExpectedArgumentError>("testcustomparser");
            ParseError<ExpectedArgumentError>("testcustomparser ");

            // parser overrides integer parsing. Completion parser falls back to normal integer parsing
            AssertResult("testcustomvarrefparser 20", 1);
            AssertResult("testcustomparser 20", 1);

            // Custom parser is used to generate completion options
            AssertCompletionContains("testcustomvarrefparser", "testcustomparser");
            AssertCompletion("testcustomvarrefparser ", new([new("A")], "B"));
            AssertCompletion("testcustomvarrefparser 2", new([new("A")], "B"));
            AssertCompletionEmpty("testcustomvarrefparser 2 ");
            AssertCompletionContains("testcustomparser", "testcustomparser");
            AssertCompletion("testcustomparser ", new([new("A")], "B"));
            AssertCompletion("testcustomparser 2", new([new("A")], "B"));
            AssertCompletionEmpty("testcustomparser 2 ");

            // custom Parsers still support variables and blocks, unless explicitly forbidden
            AssertResult("testcustomvarrefparser { i 20 }", 20);
            AssertResult("testcustomvarrefparser $x", -1);

            ParseError<ExpectedNumericError>("testcustomparser { i 20 }");
            ParseError<ExpectedNumericError>("testcustomparser $x");

            // Variable and block completions still work
            AssertCompletionSingle("testcustomvarrefparser $", "$x");
            AssertCompletionSingle("testcustomvarrefparser $x", "$x");

            AssertCompletionSingle("testcustomvarrefparser { i 2 ", "}");
            AssertCompletionSingle("testcustomvarrefparser { i 2 ", "}");
        });
    }
}
