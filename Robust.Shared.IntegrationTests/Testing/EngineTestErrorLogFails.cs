using NUnit.Framework;
using Robust.UnitTesting.Pool;

namespace Robust.UnitTesting.Shared.Testing;


public sealed class EngineTestErrorLogFails
{
    [Test]
    [Description("Asserts that Error level logs in a test pair instance do not throw, but do assert during cleanup.")]
    public async Task AssertErrorLoggingFailsTest()
    {
        var pool = new EngineDummyTestPool();

        pool.Startup();

        var pair = await pool.GetPair();

        // Log on both sides.. nothing should happen.
        Assert.DoesNotThrow(() => pair.Server.Log.Error("Mogus"));
        Assert.DoesNotThrow(() => pair.Client.Log.Error("Mogus"));

        // But it should get very mad here.
        Assert.ThrowsAsync<MultipleAssertException>(async () => await pair.CleanReturnAsync());

        Assert.That(pair.State, NUnit.Framework.Is.EqualTo(PairState.Dead));

        pool.Shutdown();
    }
}
