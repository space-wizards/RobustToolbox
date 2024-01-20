using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
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

    protected ServerIntegrationInstance Server = default!;

    public ToolshedManager Toolshed { get; private set; } = default!;
    public ToolshedEnvironment Environment => Toolshed.DefaultEnvironment;

    protected IInvocationContext? InvocationContext = null;



    [TearDown]
    public async Task TearDownInternal()
    {
        await TearDown();
        Server.Dispose();
    }

    protected virtual async Task TearDown()
    {
        Assert.That(_expectedErrors, Is.Empty);
        ClearErrors();
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

    protected T InvokeCommand<T>(string command)
    {
        InvokeCommand(command, out var res);
        Assert.That(res, Is.AssignableTo<T>());
        return (T) res!;
    }

    protected TOut InvokeCommand<TIn, TOut>(string command, TIn input)
    {
        Toolshed.InvokeCommand(this, command, input, out var res);
        Assert.That(res, Is.AssignableTo<TOut>());
        return (TOut) res!;
    }

    protected ParserContext Parser(string input) => new ParserContext(input, Toolshed);

    protected void AssertParseable<T>()
    {
        Toolshed.TryParse<T>(Parser(""), out _, out var err);
        Assert.That(err, Is.Not.TypeOf<UnparseableValueError>(), $"Couldn't find a parser for {typeof(T).PrettyName()}");
    }

    protected void ParseCommand(string command, Type? inputType = null, Type? expectedType = null, bool once = false)
    {
        var parser = new ParserContext(command, Toolshed);
        var success = CommandRun.TryParse(false, parser, inputType, expectedType, once, out _, out _, out var error);

        if (error is not null)
            ReportError(error);

        if (error is null)
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

    private Queue<Type> _expectedErrors = new();

    private List<IConError> _errors = new();

    public void ReportError(IConError err)
    {
        if (_expectedErrors.Count == 0)
        {
            if (AssertOnUnexpectedError)
            {
                Assert.Fail($"Got an error, {err.GetType()}, when none was expected.\n{err.Describe()}");
            }

            goto done;
        }

        var ty = _expectedErrors.Dequeue();

        if (AssertOnUnexpectedError)
        {
            Assert.That(
                    err.GetType().IsAssignableTo(ty),
                    $"The error {err.GetType()} wasn't assignable to the expected type {ty}.\n{err.Describe()}"
                );
        }

        done:
        _errors.Add(err);
    }

    public IEnumerable<IConError> GetErrors()
    {
        return _errors;
    }

    public void ClearErrors()
    {
        _errors.Clear();
    }

    public Dictionary<string, object?> Variables { get; } = new();

    protected void ExpectError(Type err)
    {
        _expectedErrors.Enqueue(err);
    }

    protected void ExpectError<T>()
    {
        _expectedErrors.Enqueue(typeof(T));
    }
}
