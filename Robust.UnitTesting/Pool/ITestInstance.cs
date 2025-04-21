using System;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Robust.UnitTesting.Pool;

public interface ITestInstance : IDisposable
{
    bool IsAlive { get; }

    Exception? UnhandledException { get; }

    EntityManager EntMan { get; }
    IPrototypeManager ProtoMan { get; }
    IConfigurationManager CfgMan { get; }
    ISharedPlayerManager PlayerMan { get; }
    INetManager NetMan { get; }
    IGameTiming Timing { get; }
    ISawmill Log { get; }

    T Resolve<T>();

    TransformComponent Transform(EntityUid uid);
    MetaDataComponent MetaData(EntityUid uid);
    MetaDataComponent MetaData(NetEntity uid);
    TransformComponent Transform(NetEntity uid);

    Task ExecuteCommand(string cmd);
    Task WaitIdleAsync(bool throwOnUnhandled = true, CancellationToken cancellationToken = default);
    void RunTicks(int ticks);
    Task WaitRunTicks(int ticks);

    void Post(Action post);
    Task WaitPost(Action post);

    void Assert(Action assertion);
    Task WaitAssertion(Action assertion);

    /// <summary>
    /// Post-test cleanup
    /// </summary>
    Task Cleanup();
}

public interface IClientTestInstance : ITestInstance
{
    IClientNetManager CNetMan => (IClientNetManager) NetMan;
    ICommonSession? Session { get; }
    NetUserId? User { get; }
    EntityUid? AttachedEntity { get; }
    Task Connect(IServerTestInstance target);
}

public interface IServerTestInstance : ITestInstance;
