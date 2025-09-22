using NUnit.Framework;
using Robust.Client.Console;
using Robust.Server.Console;
using Robust.Shared.Player;

namespace Robust.UnitTesting;

internal sealed class TestingServerConsoleHost : ServerConsoleHost
{
    public override void WriteError(ICommonSession? session, string text)
    {
        base.WriteError(session, text);
        Assert.Fail($"Console command encountered an error: {text}");
    }
}

internal sealed class TestingClientConsoleHost : ClientConsoleHost
{
    public override void WriteError(ICommonSession? session, string text)
    {
        base.WriteError(session, text);
        Assert.Fail($"Console command encountered an error: {text}");
    }
}
