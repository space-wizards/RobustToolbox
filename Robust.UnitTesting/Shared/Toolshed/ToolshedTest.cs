using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.UnitTesting.Shared.Toolshed;

[TestFixture, Parallelizable(ParallelScope.Fixtures)]
[FixtureLifeCycle(LifeCycle.SingleInstance)]
public abstract class ToolshedTest : RobustIntegrationTest, IInvocationContext
{
    protected virtual bool AssertOnUnexpectedError => true;

#pragma warning disable NUnit1032
    protected ServerIntegrationInstance Server = default!;
#pragma warning restore NUnit1032

    public ToolshedManager Toolshed { get; private set; } = default!;
    public ToolshedEnvironment Environment => Toolshed.DefaultEnvironment;

    protected IInvocationContext? InvocationContext = null;

    [TearDown]
    public async Task TearDownInternal()
    {
        await TearDown();
        await ReturnToPool(Server);
        Server = default!;
    }

    protected virtual Task TearDown()
    {
        Assert.That(ExpectedErrors, Is.Empty);
        ClearErrors();
        return Task.CompletedTask;
    }

    [SetUp]
    public virtual async Task Setup()
    {
        var options = new ServerIntegrationOptions()
        {
            Pool = true
        };
        Server = StartServer(options);

        await Server.WaitIdleAsync();

        Toolshed = Server.ResolveDependency<ToolshedManager>();
    }

    protected bool InvokeCommand(string command, out object? result)
    {
        return Toolshed.InvokeCommand(this, command, null, out result);
    }

    protected CompletionResult? GetCompletions(string cmd)
    {
        return Toolshed.GetCompletions(this, cmd);
    }

    protected object? InvokeCommand(string command, Type output)
    {
        Assert.That(InvokeCommand(command, out var res));
        Assert.That(res, Is.AssignableTo(output));
        return res;
    }

    protected T InvokeCommand<T>(string command)
    {
        return (T) InvokeCommand(command, typeof(T))!;
    }

    protected object? InvokeCommand<TIn>(string command, TIn input, Type output)
    {
        Assert.That(Toolshed.InvokeCommand(this, command, input, out var res));
        Assert.That(res, Is.AssignableTo(output));
        return res;
    }

    protected TOut InvokeCommand<TIn, TOut>(string command, TIn input)
    {
        return (TOut) InvokeCommand(command, input, typeof(TOut))!;
    }

    protected void AssertResult(string command, object? expected)
    {
        Assert.That(InvokeCommand(command, out var result));
        if (expected is IEnumerable @enum)
            Assert.That(result, Is.EquivalentTo(@enum));
        else
            Assert.That(result, Is.EqualTo(expected));
    }

    protected void AssertParseable<T>()
    {
        var parser = new ParserContext("", Toolshed, Toolshed.DefaultEnvironment, IVariableParser.Empty, null);
        Toolshed.TryParse<T>(parser, out _);
        Assert.That(parser.Error, Is.Not.TypeOf<UnparseableValueError>(), $"Couldn't find a parser for {typeof(T).PrettyName()}");
    }

    #region Completion Asserts

    protected void AssertCompletion(string command, CompletionResult? expected)
    {
        var result = GetCompletions(command);
        if (expected == null)
        {
            Assert.That(result, Is.Null);
            return;
        }

        Assert.That(result, Is.Not.Null);
        if (result == null)
            return;

        Assert.That(result.Hint, Is.EqualTo(expected.Hint));
        Assert.That(result.Options, Is.EquivalentTo(expected.Options));
    }

    protected void AssertCompletionEmpty(string command)
    {
        var result = GetCompletions(command);
        if (result == null)
            return;

        Assert.That(result.Options.Length, Is.EqualTo(0));
        Assert.That(result.Hint, Is.Null);
    }

    protected void AssertCompletionHint(string command, string hint)
    {
        var result = GetCompletions(command);
        Assert.That(result, Is.Not.Null);
        if (result == null)
            return;

        Assert.That(result.Hint, Is.EqualTo(hint));
    }

    protected void AssertCompletionSingle(string command, string expected)
    {
        var result = GetCompletions(command);
        Assert.That(result, Is.Not.Null);
        if (result == null)
            return;

        Assert.That(result.Options.Length, Is.EqualTo(1));
        if (result.Options.Length != 1)
            return;

        Assert.That(result.Options[0].Value, Is.EqualTo(expected));
    }

    protected void AssertCompletionContains(string command, params string[] expected)
    {
        var result = GetCompletions(command);

        Assert.That(result, Is.Not.Null);
        if (result == null)
            return;

        Assert.That(result.Options.Length, Is.GreaterThanOrEqualTo(expected.Length));
        if (result.Options.Length != 1)
            return;

        foreach (var ex in expected)
        {
            Assert.That(result.Options.Any(x => x.Value == ex));
        }
    }

    /// <summary>
    /// Check that the given string is not a suggested completion option.
    /// </summary>
    protected void AssertCompletionInvalid(string command, string invalid)
    {
        var result = GetCompletions(command);
        if (result == null)
            return;

        foreach (var res in result.Options)
        {
            Assert.That(res.Value, Is.Not.EqualTo(invalid));
        }
    }


    #endregion

    protected void ParseCommand(string command, Type? inputType = null, Type? expectedType = null)
    {
        var parser = new ParserContext(command, Toolshed, Toolshed.DefaultEnvironment, IVariableParser.Empty, null);
        var success = CommandRun.TryParse(parser, inputType, expectedType, out _);
        ReportError(parser.Error);

        if (parser.Error is null)
            Assert.That(success, $"Parse failed despite no error being reported. Parsed {command}");
    }

    public bool CheckInvokable(CommandSpec command, out IConError? error)
    {
        if (InvocationContext is not null)
        {
            return InvocationContext.CheckInvokable(command, out error);
        }

        error = null;
        return true;
    }

    protected ICommonSession? InvocationSession { get; set; }

    public NetUserId? User => Session?.UserId;

    public ICommonSession? Session
    {
        get
        {
            if (InvocationContext is not null)
            {
                return InvocationContext.Session;
            }

            return InvocationSession;
        }
    }

    public void WriteLine(string line)
    {
        return;
    }

    protected Queue<Type> ExpectedErrors = new();

    protected List<IConError> Errors = new();

    public void ReportError(IConError? err)
    {
        if (err == null)
        {
            if (ExpectedErrors.Count > 0)
                Assert.Fail($"Expected an error of type {ExpectedErrors.Peek().PrettyName()}, but none was received");
            return;
        }

        if (ExpectedErrors.Count == 0)
        {
            if (AssertOnUnexpectedError)
            {
                Assert.Fail($"Got an error, {err.GetType()}, when none was expected.\n{err.Describe()}");
            }

            goto done;
        }

        var ty = ExpectedErrors.Dequeue();

        if (AssertOnUnexpectedError)
        {
            Assert.That(
                    err.GetType().IsAssignableTo(ty),
                    $"The error {err.GetType()} wasn't assignable to the expected type {ty}.\n{err.Describe()}"
                );
        }

        done:
        Errors.Add(err);
    }

    public IEnumerable<IConError> GetErrors()
    {
        return Errors;
    }

    public bool HasErrors => Errors.Count > 0;

    public void ClearErrors()
    {
        Errors.Clear();
    }

    /// <inheritdoc />
    public object? ReadVar(string name)
    {
        return Variables.GetValueOrDefault(name);
    }

    /// <inheritdoc />
    public void WriteVar(string name, object? value)
    {
        Variables[name] = value;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetVars()
    {
        return Variables.Keys;
    }

    public Dictionary<string, object?> Variables { get; } = new();

    protected void ExpectError(Type err)
    {
        ExpectedErrors.Enqueue(err);
    }

    protected void ExpectError<T>() where T : IConError
    {
        ExpectError(typeof(T));
    }

    protected void ParseError<T>(string cmd) where T : IConError
    {
        ExpectError<T>();
        ParseCommand(cmd);
    }
}
