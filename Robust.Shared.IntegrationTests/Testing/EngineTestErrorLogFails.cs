using NUnit.Framework;

namespace Robust.UnitTesting.Shared.Testing;


public sealed class EngineTestErrorLogFails
{
    [Test]
    public async Task AssertErrorLoggingFailsTest()
    {
        var pool = new EngineDummyTestPool();

        pool.Startup();

        var pair = await pool.GetPair();

        Assert.DoesNotThrow(() => pair.Server.Log.Error("Mogus"));
        Assert.DoesNotThrow(() => pair.Client.Log.Error("Mogus"));

        Assert.ThrowsAsync<MultipleAssertException>(async () => await pair.CleanReturnAsync());

        pool.Shutdown();
    }
}
