using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Robust.UnitTesting;

public interface IIntegrationInstance : IDisposable
{
    /// <summary>
    ///     Whether the instance is still alive.
    ///     "Alive" indicates that it is able to receive and process commands.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if you did not ensure that the instance is idle via <see cref="WaitIdleAsync"/> first.
    /// </exception>
    bool IsAlive { get; }

    Exception? UnhandledException { get; }

    EntityManager EntMan { get; }
    IPrototypeManager ProtoMan { get; }
    IConfigurationManager CfgMan { get; }
    ISharedPlayerManager PlayerMan { get; }
    INetManager NetMan { get; }
    IMapManager MapMan { get; }
    IGameTiming Timing { get; }
    ISawmill Log { get; }

    /// <summary>
    ///     Resolve a dependency inside the instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if you did not ensure that the instance is idle via <see cref="WaitIdleAsync"/> first.
    /// </exception>
    [Pure] T Resolve<T>();

    [Pure] T System<T>() where T : IEntitySystem;

    TransformComponent Transform(EntityUid uid);
    MetaDataComponent MetaData(EntityUid uid);
    MetaDataComponent MetaData(NetEntity uid);
    TransformComponent Transform(NetEntity uid);

    Task ExecuteCommand(string cmd);

    /// <summary>
    ///     Wait for the instance to go idle, either through finishing all commands or shutting down/crashing.
    /// </summary>
    /// <param name="throwOnUnhandled">
    ///     If true, throw an exception if the server dies on an unhandled exception.
    /// </param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="Exception">
    ///     Thrown if <paramref name="throwOnUnhandled"/> is true and the instance shuts down on an unhandled exception.
    /// </exception>
    Task WaitIdleAsync(bool throwOnUnhandled = true, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Queue for the server to run n ticks.
    /// </summary>
    /// <param name="ticks">The amount of ticks to run.</param>
    void RunTicks(int ticks);

    /// <summary>
    ///     <see cref="RunTicks"/> followed by <see cref="WaitIdleAsync"/>
    /// </summary>
    Task WaitRunTicks(int ticks);

    /// <summary>
    ///     Queue for a delegate to be ran inside the main loop of the instance.
    /// </summary>
    /// <remarks>
    ///     Do not run NUnit assertions inside <see cref="Post"/>. Use <see cref="Assert"/> instead.
    /// </remarks>
    void Post(Action post);

    /// <inheritdoc cref="Post"/>
    Task WaitPost(Action post);

    /// <summary>
    ///     Queue for a delegate to be ran inside the main loop of the instance,
    ///     rethrowing any exceptions in <see cref="WaitIdleAsync"/>.
    /// </summary>
    /// <remarks>
    ///     Exceptions raised inside this callback will be rethrown by <see cref="WaitIdleAsync"/>.
    ///     This makes it ideal for NUnit assertions,
    ///     since rethrowing the NUnit assertion directly provides less noise.
    /// </remarks>
    void Assert(Action assertion);

    /// <inheritdoc cref="Assert"/>
    Task WaitAssertion(Action assertion);

    /// <summary>
    /// Post-test cleanup
    /// </summary>
    Task Cleanup();
}

public interface IClientIntegrationInstance : IIntegrationInstance
{
    IClientNetManager CNetMan => (IClientNetManager) NetMan;
    ICommonSession? Session { get; }
    NetUserId? User { get; }
    EntityUid? AttachedEntity { get; }
    Task Connect(IServerIntegrationInstance target);
}

public interface IServerIntegrationInstance : IIntegrationInstance;
