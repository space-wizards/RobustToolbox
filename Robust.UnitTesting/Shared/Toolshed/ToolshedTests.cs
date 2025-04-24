using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
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
            ParseError<WrongCommandReturn>("i 2 + { i 2; }");
            ParseError<WrongCommandReturn>("i 2 + { i 2; ; } ;; ;");
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
    public async Task TestTerminators()
    {
        await Server.WaitAssertion(() =>
        {
            // Baseline check that these commands work:
            AssertResult("i 1", 1);
            AssertResult("i 1 + 1", 2);
            AssertResult("i { i 1 }", 1);

            // Trailing terminators have no clear effect.
            AssertResult("i 1;", 1);
            AssertResult("i { i 1 };", 1);

            // Simple explicit piping works
            AssertResult("i 1 | + 1", 2);

            // Explicit pipes imply a command is expected. Ending a command or a block after a pipe should error.
            ParseError<OutOfInputError>("i 1 |");
            ParseError<UnexpectedCloseBrace>("i { i 1 | }");

            // A terminator inside a block or command run doesn't pipe anything;
            ParseError<NoImplementationError>("i 1 ; + 1");
            ParseError<WrongCommandReturn>("i { i 1 ; }");

            // Check double terminators
            // A starting terminators/pipes will try to be parsed as a command.
            ParseError<NotValidCommandError>("|");
            ParseError<NotValidCommandError>(";");
            ParseError<NotValidCommandError>(";;");
            ParseError<NotValidCommandError>("||");
            ParseError<NotValidCommandError>("|;");
            ParseError<NotValidCommandError>(";|");
            AssertResult("i 1 ;;", 1);

            // Consecutive pipes will try to parse the second one as the command, which will not succeed.
            ParseError<NotValidCommandError>("i 1 ||");
            ParseError<NotValidCommandError>("i 1 |;");
            ParseError<NotValidCommandError>("i 1 ;|");
            AssertResult("i 1 ;; i 1", 1);
            ParseError<NotValidCommandError>("i 1 || i 1");
            ParseError<NotValidCommandError>("i 1 |; i 1");
            ParseError<NotValidCommandError>("i 1 ;| i 1");
            ParseError<NoImplementationError>("i 1 ;; + 1");
            ParseError<NotValidCommandError>("i 1 || + 1");
            ParseError<NotValidCommandError>("i 1 |; + 1");
            ParseError<NotValidCommandError>("i 1 ;| + 1");
        });
    }

    [Test]
    public async Task TestOptionalArgs()
    {
        await Server.WaitAssertion(() =>
        {
            // Check that straightforward optional args work.
            ParseError<ExpectedArgumentError>("testoptionalargs ");
            AssertResult("testoptionalargs 1", new[] {1, 0, 1});
            AssertResult("testoptionalargs 1 2", new[] {1, 2, 1});
            AssertResult("testoptionalargs 1 2 3", new[] {1, 2, 3});
            AssertResult("testoptionalargs 1 2 3 append 4", new[] {1, 2, 3, 4});
            ParseError<UnknownCommandError>("testoptionalargs 1 2 3 4");
            ParseError<InvalidNumber<int>>("testoptionalargs 1 append 4");
            ParseError<InvalidNumber<int>>("testoptionalargs 1 2 append 4");

            // Check that semicolon terminators interrupt optional args
            ParseError<ExpectedArgumentError>("testoptionalargs ;");
            AssertResult("testoptionalargs 1;", new[] {1, 0, 1});
            AssertResult("testoptionalargs 1 2;", new[] {1, 2, 1});
            AssertResult("testoptionalargs 1 2 3;", new[] {1, 2, 3});
            ParseError<UnknownCommandError>("testoptionalargs 1 2 3; 4");
            AssertResult("testoptionalargs 1 2; i 3", 3);
            AssertResult("testoptionalargs 1 2 3; i 4", 4);

            // Check that explicit pipes interrupt optional args
            ParseError<ExpectedArgumentError>("testoptionalargs |");
            ParseError<OutOfInputError>("testoptionalargs 1 |");
            AssertResult("testoptionalargs 1 | append 4", new[] {1, 0, 1, 4});
            AssertResult("testoptionalargs 1 2 | append 4", new[] {1, 2, 1, 4});
            AssertResult("testoptionalargs 1 2 3 | append 4", new[] {1, 2, 3, 4});

            // Check that variables and blocks can be used to specify optional args;
            AssertResult("i -1 => $i", -1);
            AssertResult("testoptionalargs 1 $i", new[] {1, -1, 1});
            AssertResult("testoptionalargs 1 $i 2", new[] {1, -1, 2});
            AssertResult("testoptionalargs 1 { i -1 }", new[] {1, -1, 1});
            AssertResult("testoptionalargs 1 { i -1 } 2", new[] {1, -1, 2});

            // Repeat the above groups of tests, but within a command block.
            // I.e., wrap the commands in "i 1 join { <old command> }" to prepend "1" to the results.

            // This first block also effectively checks that closing braces can interrupt optional args
            ParseError<ExpectedArgumentError>("i 1 join { testoptionalargs } ");
            AssertResult("i 1 join { testoptionalargs 1 } ", new[] {1, 1, 0, 1});
            AssertResult("i 1 join { testoptionalargs 1 2 }", new[] {1, 1, 2, 1});
            AssertResult("i 1 join { testoptionalargs 1 2 3 }", new[] {1, 1, 2, 3});
            AssertResult("i 1 join { testoptionalargs 1 2 3 append 4 }", new[] {1, 1, 2, 3, 4});
            ParseError<UnknownCommandError>("testoptionalargs 1 2 3 4 }");
            ParseError<InvalidNumber<int>>("testoptionalargs 1 2 i 3 }");
            ParseError<NoImplementationError>("testoptionalargs 1 2 3 i 4 }");

            ParseError<ExpectedArgumentError>("i 1 join { testoptionalargs | }");
            ParseError<UnexpectedCloseBrace>("i 1 join { testoptionalargs 1 | }");
            AssertResult("i 1 join { testoptionalargs 1 | append 4 }", new[] {1, 1, 0, 1, 4});
            AssertResult("i 1 join { testoptionalargs 1 2 | append 4 }", new[] {1, 1, 2, 1, 4});
            AssertResult("i 1 join { testoptionalargs 1 2 3 | append 4 }", new[] {1, 1, 2, 3, 4});

            AssertResult("i 1 join { testoptionalargs 1 $i }", new[] {1, 1, -1, 1});
            AssertResult("i 1 join { testoptionalargs 1 $i 2 }", new[] {1, 1, -1, 2});
            AssertResult("i 1 join { testoptionalargs 1 { i -1 } }", new[] {1, 1, -1, 1});
            AssertResult("i 1 join { testoptionalargs 1 { i -1 } 2 }", new[] {1, 1, -1, 2});
        });
    }

    [Test]
    public async Task TestParamsCollections()
    {
        await Server.WaitAssertion(() =>
        {
            // Check that straightforward optional args work.
            ParseError<ExpectedArgumentError>("testparamscollection");
            AssertResult("testparamsonly", new int[] {});
            AssertResult("testparamscollection 1", new[] {1, 0});
            AssertResult("testparamscollection 1 2", new[] {1, 2});
            AssertResult("testparamscollection 1 2 3", new[] {1, 2, 3});
            AssertResult("testparamscollection 1 2 3 4", new[] {1, 2, 3, 4});
            ParseError<InvalidNumber<int>>("testparamscollection 1 2 append 4");
            ParseError<InvalidNumber<int>>("testparamscollection 1 2 3 append 4");
            ParseError<InvalidNumber<int>>("testparamscollection 1 2 3 4 append 4");

            // Check that semicolon terminators interrupt optional args
            ParseError<ExpectedArgumentError>("testparamscollection ;");
            AssertResult("testparamsonly;", new int[] { });
            AssertResult("testparamscollection 1;", new[] {1, 0});
            AssertResult("testparamscollection 1 2;", new[] {1, 2});
            AssertResult("testparamscollection 1 2 3;", new[] {1, 2, 3});
            AssertResult("testparamscollection 1 2 3 4;", new[] {1, 2, 3, 4});
            AssertResult("testparamscollection 1 2; i 4", 4);
            AssertResult("testparamscollection 1 2 3; i 4", 4);
            AssertResult("testparamscollection 1 2 3 4; i 4", 4);

            // Check that explicit pipes interrupt optional args
            ParseError<ExpectedArgumentError>("testparamscollection |");
            ParseError<OutOfInputError>("testparamsonly |");
            ParseError<OutOfInputError>("testparamscollection 1 |");
            ParseError<OutOfInputError>("testparamscollection 1 2 |");
            ParseError<OutOfInputError>("testparamscollection 1 2 3 |");
            ParseError<OutOfInputError>("testparamscollection 1 2 3 4 |");
            AssertResult("testparamsonly | append 1", new[] {1});
            AssertResult("testparamscollection 1 | append 1", new[] {1, 0, 1});
            AssertResult("testparamscollection 1 2 | append 1", new[] {1, 2, 1});
            AssertResult("testparamscollection 1 2 3 | append 1", new[] {1, 2, 3, 1});
            AssertResult("testparamscollection 1 2 3 4 | append 1", new[] {1, 2, 3, 4, 1});

            // Check that variables and blocks can be used to specify args inside params arrays;
            AssertResult("i -1 => $i", -1);
            AssertResult("testparamscollection 1 2 3 $i 5", new[] {1, 2, 3, -1, 5});
            AssertResult("testparamscollection 1 2 3 { i -1 } 5", new[] {1, 2, 3, -1, 5});

            // Check that closing braces interrupt optional args
            AssertResult("i 1 join { testparamsonly }", new[] {1});
            AssertResult("i 1 join { testparamscollection 1 }", new[] {1, 1, 0});
            AssertResult("i 1 join { testparamscollection 1 2 }", new[] {1, 1, 2});
            AssertResult("i 1 join { testparamscollection 1 2 3 }", new[] {1, 1, 2, 3});
            AssertResult("i 1 join { testparamscollection 1 2 3 4 }", new[] {1, 1, 2, 3, 4});
        });
    }

    /// <summary>
    /// Check that the type of generic parameters can be correctly inferred from the piped-in value. I.e., when check
    /// that if we pipe a <see cref="List{T}"/> into a command that takes an <see cref="IEnumerable{T}"/>, the value of
    /// the generic parameter can be properly inferred.
    /// </summary>
    [Test]
    [TestOf(typeof(TakesPipedTypeAsGenericAttribute))]
    public async Task TestGenericPipeInference()
    {
        await Server.WaitAssertion(() =>
        {
            // Pipe T[] -> T[]
            AssertResult("testarray testarrayinfer 1", typeof(int));

            // Pipe List<T> -> List<T>
            AssertResult("testlist testlistinfer 1", typeof(int));

            // Pipe T[] -> IEnumerable<T>
            AssertResult("testarray testenumerableinfer 1", typeof(int));

            // Pipe List<T> -> IEnumerable<T>
            AssertResult("testlist testenumerableinfer 1", typeof(int));

            // Pipe IEnumerable<T> -> IEnumerable<T>
            AssertResult("testenumerable testenumerableinfer 1", typeof(int));

            // Repeat but with nested types. i.e. extracting T when piping ProtoId<T> -> IEnumerable<ProtoId<T>>
            AssertResult("testnestedarray testnestedarrayinfer", typeof(EntityCategoryPrototype));
            AssertResult("testnestedlist testnestedlistinfer", typeof(EntityCategoryPrototype));
            AssertResult("testnestedarray testnestedenumerableinfer", typeof(EntityCategoryPrototype));
            AssertResult("testnestedlist testnestedenumerableinfer", typeof(EntityCategoryPrototype));
            AssertResult("testnestedenumerable testnestedenumerableinfer", typeof(EntityCategoryPrototype));

            // The map command used to work when the piped type was passed as an IEnumerable<T> directly, but would fail
            // when given a List<T> or something else that implemented the interface.
            // In particular, this would become relevant when using command variables (which store enumerables as a List<T>).
            AssertResult("i 1 to 4 map { * 2 }", new[] {2, 4, 6, 8});
            InvokeCommand("i 1 to 4 => $x", out _);
            AssertResult("var $x map { * 2 }", new[] {2, 4, 6, 8});
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
            AssertCompletionHint("i 1", "<value (Int32)>");
            AssertCompletionSingle($"i 1 => $x", "$x");
            AssertCompletionContains($"testvoid", "testvoid");

            // If an error occurs while parsing something, but tha error is not at the end of the command, we should
            // not generate completions. I.e., we don't want to mislead people into thinking a command is valid and is
            // expecting additional arguments.
            AssertCompletionHint("i a", "<value (Int32)>");
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
            AssertCompletionHint("testintstrarg ", "<i (Int32)>");
            AssertCompletionHint("testintstrarg 1", "<i (Int32)>");
            AssertCompletionSingle("testintstrarg 1 ", "\"");
            AssertCompletionHint("testintstrarg 1 \"", "<str (String)>");
            AssertCompletionHint("testintstrarg 1 \"a\"", "<str (String)>");
            AssertCompletionEmpty("testintstrarg 1 \"a\" ");
            AssertCompletionHint("testintstrarg 1 \"a\" + ", "<y (Int32)>");

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
