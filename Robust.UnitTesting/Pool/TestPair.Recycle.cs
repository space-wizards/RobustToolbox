using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client;
using Robust.Shared;
using Robust.Shared.Exceptions;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Robust.UnitTesting.Pool;

// This partial file contains logic related to recycling & disposing test pairs.
public partial class TestPair<TServer, TClient>
{
    private async Task OnDirtyDispose()
    {
        var usageTime = Watch.Elapsed;
        Watch.Restart();
        await TestOut.WriteLineAsync($"{nameof(DisposeAsync)}: Test gave back pair {Id} in {usageTime.TotalMilliseconds} ms");
        Kill();
        var disposeTime = Watch.Elapsed;
        await TestOut.WriteLineAsync($"{nameof(DisposeAsync)}: Disposed pair {Id} in {disposeTime.TotalMilliseconds} ms");
        // Test pairs should only dirty dispose if they are failing. If they are not failing, this probably happened
        // because someone forgot to clean-return the pair.
        Assert.Warn("Test was dirty-disposed.");
    }

    /// <summary>
    /// This method gets called before any test pair gets returned to the pool.
    /// </summary>
    protected virtual async Task Cleanup()
    {
        if (TestMap != null)
        {
            await Server.WaitPost(() => Server.EntMan.DeleteEntity(TestMap.MapUid));
            TestMap = null;
        }
    }

    private async Task OnCleanDispose()
    {
        await Server.WaitIdleAsync();
        await Client.WaitIdleAsync();
        await Cleanup();
        await Server.Cleanup();
        await Client.Cleanup();
        await RevertModifiedCvars();

        var usageTime = Watch.Elapsed;
        Watch.Restart();
        await TestOut.WriteLineAsync($"{nameof(CleanReturnAsync)}: Test borrowed pair {Id} for {usageTime.TotalMilliseconds} ms");
        // Let any last minute failures the test cause happen.
        await ReallyBeIdle();
        if (!Settings.Destructive)
        {
            if (Client.IsAlive == false)
                throw new Exception($"{nameof(CleanReturnAsync)}: Test killed the client in pair {Id}:", Client.UnhandledException);

            if (Server.IsAlive == false)
                throw new Exception($"{nameof(CleanReturnAsync)}: Test killed the server in pair {Id}:", Server.UnhandledException);
        }

        if (Settings.MustNotBeReused)
        {
            Kill();
            await ReallyBeIdle();
            await TestOut.WriteLineAsync($"{nameof(CleanReturnAsync)}: Clean disposed in {Watch.Elapsed.TotalMilliseconds} ms");
            return;
        }

        var sRuntimeLog = Server.Resolve<IRuntimeLog>();
        if (sRuntimeLog.ExceptionCount > 0)
            throw new Exception($"{nameof(CleanReturnAsync)}: Server logged exceptions");
        var cRuntimeLog = Client.Resolve<IRuntimeLog>();
        if (cRuntimeLog.ExceptionCount > 0)
            throw new Exception($"{nameof(CleanReturnAsync)}: Client logged exceptions");

        var returnTime = Watch.Elapsed;
        await TestOut.WriteLineAsync($"{nameof(CleanReturnAsync)}: PoolManager took {returnTime.TotalMilliseconds} ms to put pair {Id} back into the pool");
    }

    public async ValueTask CleanReturnAsync()
    {
        if (State != PairState.InUse)
            throw new Exception($"{nameof(CleanReturnAsync)}: Unexpected state. Pair: {Id}. State: {State}.");

        await TestOut.WriteLineAsync($"{nameof(CleanReturnAsync)}: Return of pair {Id} started");
        State = PairState.CleanDisposed;
        await OnCleanDispose();
        State = PairState.Ready;
        Manager.Return(this);
        ClearContext();
    }

    public async ValueTask DisposeAsync()
    {
        switch (State)
        {
            case PairState.Dead:
            case PairState.Ready:
                break;
            case PairState.InUse:
                await TestOut.WriteLineAsync($"{nameof(DisposeAsync)}: Dirty return of pair {Id} started");
                await OnDirtyDispose();
                Manager.Return(this);
                ClearContext();
                break;
            default:
                throw new Exception($"{nameof(DisposeAsync)}: Unexpected state. Pair: {Id}. State: {State}.");
        }
    }

    /// <summary>
    /// This method gets called when a previously used test pair is being retrieved from the pool.
    /// Note that in some instances this method may get skipped (See <see cref="PairSettings.CanFastRecycle"/>).
    /// </summary>
    public async Task RecycleInternal(PairSettings settings, TextWriter testOut)
    {
        Watch.Restart();
        await testOut.WriteLineAsync($"Recycling...");
        await RunTicksSync(1);

        // Disconnect the client if they are connected.
        if (Client.CNetMan.IsConnected)
        {
            await testOut.WriteLineAsync($"Recycling: {Watch.Elapsed.TotalMilliseconds} ms: Disconnecting client.");
            await Client.WaitPost(() => Client.CNetMan.ClientDisconnect("Test pooling cleanup disconnect"));
            await RunTicksSync(1);
        }

        await Recycle(settings, testOut);
        ClearModifiedCvars();

        // (possibly) reconnect the client
        if (settings.Connected)
        {
            await testOut.WriteLineAsync($"Recycling: {Watch.Elapsed.TotalMilliseconds} ms: Connecting client");
            await Client.Connect(Server);
        }

        Settings = default!;
        await testOut.WriteLineAsync($"Recycling: {Watch.Elapsed.TotalMilliseconds} ms: Idling");
        await ReallyBeIdle();
        await testOut.WriteLineAsync($"Recycling: {Watch.Elapsed.TotalMilliseconds} ms: Done recycling");
    }

    /// <summary>
    /// This method gets called when a previously used test pair is being retrieved from the pool.
    /// If the next settings are compatible with the previous settings, this step may get skipped (See <see cref="PairSettings.CanFastRecycle"/>).
    /// In general, this method should also call <see cref="ApplySettings"/>.
    /// </summary>
    protected virtual async Task Recycle(PairSettings next, TextWriter testOut)
    {
        //Apply Cvars
        await testOut.WriteLineAsync($"Recycling: {Watch.Elapsed.TotalMilliseconds} ms: Setting CVar ");
        await ApplySettings(next);
        await RunTicksSync(1);

        // flush server entities.
        await testOut.WriteLineAsync($"Recycling: {Watch.Elapsed.TotalMilliseconds} ms: Flushing server entities");
        await Server.WaitPost(() => Server.EntMan.FlushEntities());
        await RunTicksSync(1);
    }

    /// <summary>
    /// Apply settings to the test pair. This method is always called when a pair is fetched from the pool. There should
    /// be no need to apply settings that require a pair to be recycled, as in those cases the
    /// <see cref="PairSettings.CanFastRecycle"/> should have caused <see cref="Recycle"/> to be invoked, which should
    /// already have applied those settings.
    /// </summary>
    public async Task ApplySettings(PairSettings next)
    {
        await ApplySettings(Client, next);
        await ApplySettings(Server, next);
    }

    /// <inheritdoc cref="ApplySettings(PairSettings)"/>
    [MustCallBase]
    protected internal virtual async Task ApplySettings(IIntegrationInstance instance, PairSettings next)
    {
        if (instance.CfgMan.IsCVarRegistered(CVars.NetInterp.Name))
            await instance.WaitPost(() => instance.CfgMan.SetCVar(CVars.NetInterp, !next.DisableInterpolate));
    }

    /// <summary>
    /// Invoked after a test pair has been recycled to validate that the settings have been properly applied.
    /// </summary>
    [MustCallBase]
    public virtual void ValidateSettings(PairSettings settings)
    {
        var netMan = Client.Resolve<INetManager>();
        Assert.That(netMan.IsConnected, Is.EqualTo(settings.Connected));

        if (!settings.Connected)
            return;

        var baseClient = Client.Resolve<IBaseClient>();
        var cPlayer = Client.Resolve<ISharedPlayerManager>();
        var sPlayer = Server.Resolve<ISharedPlayerManager>();

        Assert.That(baseClient.RunLevel, Is.EqualTo(ClientRunLevel.InGame));
        Assert.That(sPlayer.Sessions.Length, Is.EqualTo(1));
        var session = sPlayer.Sessions.Single();
        Assert.That(cPlayer.LocalSession?.UserId, Is.EqualTo(session.UserId));
    }
}
