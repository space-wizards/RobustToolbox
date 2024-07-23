using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Robust.Client;
using Robust.Client.Console;
using Robust.Client.GameStates;
using Robust.Client.Player;
using Robust.Client.Timing;
using Robust.Client.UserInterface;
using Robust.Server;
using Robust.Server.Console;
using Robust.Server.GameStates;
using Robust.Server.ServerStatus;
using Robust.Shared;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using ServerProgram = Robust.Server.Program;

namespace Robust.UnitTesting
{
    /// <summary>
    ///     Base class allowing you to implement integration tests.
    /// </summary>
    /// <remarks>
    ///     Integration tests allow you to act upon a running server as a whole,
    ///     contrary to unit testing which tests, well, units.
    /// </remarks>
    public abstract partial class RobustIntegrationTest
    {
        internal static readonly ConcurrentQueue<ClientIntegrationInstance> ClientsReady = new();
        internal static readonly ConcurrentQueue<ServerIntegrationInstance> ServersReady = new();

        internal static readonly ConcurrentQueue<string> ClientsCreated = new();
        internal static readonly ConcurrentQueue<string> ClientsPooled = new();
        internal static readonly ConcurrentQueue<string> ClientsNotPooled = new();

        internal static readonly ConcurrentQueue<string> ServersCreated = new();
        internal static readonly ConcurrentQueue<string> ServersPooled = new();
        internal static readonly ConcurrentQueue<string> ServersNotPooled = new();

        private readonly List<IntegrationInstance> _notPooledInstances = new();

        private readonly ConcurrentDictionary<ClientIntegrationInstance, byte> _clientsRunning = new();
        private readonly ConcurrentDictionary<ServerIntegrationInstance, byte> _serversRunning = new();

        private string TestId => TestContext.CurrentContext.Test.FullName;

        private string GetTestsRanString(IntegrationInstance instance, string running)
        {
            var type = instance is ServerIntegrationInstance ? "Server " : "Client ";

            return $"{type} tests ran ({instance.TestsRan.Count}):\n" +
                   $"{string.Join('\n', instance.TestsRan)}\n" +
                   $"Currently running: {running}";
        }

        /// <summary>
        ///     Start an instance of the server and return an object that can be used to control it.
        /// </summary>
        protected virtual ServerIntegrationInstance StartServer(ServerIntegrationOptions? options = null)
        {
            ServerIntegrationInstance instance;

            if (ShouldPool(options))
            {
                if (ServersReady.TryDequeue(out var server))
                {
                    server.PreviousOptions = server.ServerOptions;
                    server.ServerOptions = options;

                    OnServerReturn(server).Wait();

                    _serversRunning[server] = 0;
                    instance = server;
                }
                else
                {
                    instance = new ServerIntegrationInstance(options);
                    _serversRunning[instance] = 0;

                    ServersCreated.Enqueue(TestId);
                }

                ServersPooled.Enqueue(TestId);
            }
            else
            {
                instance = new ServerIntegrationInstance(options);
                _notPooledInstances.Add(instance);

                ServersCreated.Enqueue(TestId);
                ServersNotPooled.Enqueue(TestId);
            }

            var currentTest = TestContext.CurrentContext.Test.FullName;
            TestContext.Out.WriteLine(GetTestsRanString(instance, currentTest));
            instance.TestsRan.Add(currentTest);

            return instance;
        }

        /// <summary>
        ///     Start a headless instance of the client and return an object that can be used to control it.
        /// </summary>
        protected virtual ClientIntegrationInstance StartClient(ClientIntegrationOptions? options = null)
        {
            ClientIntegrationInstance instance;

            if (ShouldPool(options))
            {
                if (ClientsReady.TryDequeue(out var client))
                {
                    client.PreviousOptions = client.ClientOptions;
                    client.ClientOptions = options;

                    OnClientReturn(client).Wait();

                    _clientsRunning[client] = 0;
                    instance = client;
                }
                else
                {
                    instance = new ClientIntegrationInstance(options);
                    _clientsRunning[instance] = 0;

                    ClientsCreated.Enqueue(TestId);
                }

                ClientsPooled.Enqueue(TestId);
            }
            else
            {
                instance = new ClientIntegrationInstance(options);
                _notPooledInstances.Add(instance);

                ClientsCreated.Enqueue(TestId);
                ClientsNotPooled.Enqueue(TestId);
            }

            var currentTest = TestContext.CurrentContext.Test.FullName;
            TestContext.Out.WriteLine(GetTestsRanString(instance, currentTest));
            instance.TestsRan.Add(currentTest);

            return instance;
        }

        private bool ShouldPool(IntegrationOptions? options)
        {
            return options?.Pool ?? false;
        }

        protected virtual async Task OnInstanceReturn(IntegrationInstance instance)
        {
            await instance.WaitPost(() =>
            {
                var config = instance.InstanceDependencyCollection.Resolve<IConfigurationManagerInternal>();
                var overrides = new[]
                {
                    (RTCVars.FailureLogLevel.Name, (instance.Options?.FailureLogLevel ?? RTCVars.FailureLogLevel.DefaultValue).ToString())
                };

                config.OverrideConVars(overrides);
            });
        }

        protected virtual Task OnClientReturn(ClientIntegrationInstance client)
        {
            return OnInstanceReturn(client);
        }

        protected virtual Task OnServerReturn(ServerIntegrationInstance server)
        {
            return OnInstanceReturn(server);
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            foreach (var client in _clientsRunning.Keys)
            {
                await client.WaitIdleAsync();

                if (client.UnhandledException != null || !client.IsAlive)
                {
                    continue;
                }

                ClientsReady.Enqueue(client);
            }

            _clientsRunning.Clear();

            foreach (var server in _serversRunning.Keys)
            {
                await server.WaitIdleAsync();

                if (server.UnhandledException != null || !server.IsAlive)
                {
                    continue;
                }

                ServersReady.Enqueue(server);
            }

            _serversRunning.Clear();

            _notPooledInstances.ForEach(p => p.Stop());
            await Task.WhenAll(_notPooledInstances.Select(p => p.WaitIdleAsync()));
            _notPooledInstances.Clear();
        }

        /// <summary>
        ///     Provides control over a running instance of the client or server.
        /// </summary>
        /// <remarks>
        ///     The instance executes in another thread.
        ///     As such, sending commands to it purely queues them to be ran asynchronously.
        ///     To ensure that the instance is idle, i.e. not executing code and finished all queued commands,
        ///     you can use <see cref="WaitIdleAsync"/>.
        ///     This method must be used before trying to access any state like <see cref="ResolveDependency{T}"/>,
        ///     to prevent race conditions.
        /// </remarks>
        public abstract class IntegrationInstance : IDisposable
        {
            private protected Thread? InstanceThread;
            private protected IDependencyCollection DependencyCollection = default!;
            private protected IntegrationGameLoop GameLoop = default!;

            private protected readonly ChannelReader<object> _toInstanceReader;
            private protected readonly ChannelWriter<object> _toInstanceWriter;
            private protected readonly ChannelReader<object> _fromInstanceReader;
            private protected readonly ChannelWriter<object> _fromInstanceWriter;

            private protected TextWriter _testOut;

            private int _currentTicksId = 1;
            private int _ackTicksId;

            private bool _isSurelyIdle;
            private bool _isAlive = true;
            private Exception? _unhandledException;

            public IDependencyCollection InstanceDependencyCollection => DependencyCollection;

            public virtual IntegrationOptions? Options { get; internal set; }

            public IEntityManager EntMan { get; private set; } = default!;
            public IPrototypeManager ProtoMan { get; private set; } = default!;
            public IConfigurationManager CfgMan { get; private set; } = default!;
            public ISharedPlayerManager PlayerMan { get; private set; } = default!;
            public IGameTiming Timing { get; private set; } = default!;
            public IMapManager MapMan { get; private set; } = default!;
            public IConsoleHost ConsoleHost { get; private set; } = default!;
            public ISawmill Log { get; private set; } = default!;

            protected virtual void ResolveIoC(IDependencyCollection deps)
            {
                EntMan = deps.Resolve<IEntityManager>();
                ProtoMan = deps.Resolve<IPrototypeManager>();
                CfgMan = deps.Resolve<IConfigurationManager>();
                PlayerMan = deps.Resolve<ISharedPlayerManager>();
                Timing = deps.Resolve<IGameTiming>();
                MapMan = deps.Resolve<IMapManager>();
                ConsoleHost = deps.Resolve<IConsoleHost>();
                Log = deps.Resolve<ILogManager>().GetSawmill("test");
            }

            public T System<T>() where T : IEntitySystem
            {
                return EntMan.System<T>();
            }

            public TransformComponent Transform(EntityUid uid)
            {
                return EntMan.GetComponent<TransformComponent>(uid);
            }

            public MetaDataComponent MetaData(EntityUid uid)
            {
                return EntMan.GetComponent<MetaDataComponent>(uid);
            }

            public MetaDataComponent MetaData(NetEntity uid)
                => MetaData(EntMan.GetEntity(uid));

            public TransformComponent Transform(NetEntity uid)
                => Transform(EntMan.GetEntity(uid));

            public async Task ExecuteCommand(string cmd)
            {
                await WaitPost(() => ConsoleHost.ExecuteCommand(cmd));
            }

            /// <summary>
            ///     Whether the instance is still alive.
            ///     "Alive" indicates that it is able to receive and process commands.
            /// </summary>
            /// <exception cref="InvalidOperationException">
            ///     Thrown if you did not ensure that the instance is idle via <see cref="WaitIdleAsync"/> first.
            /// </exception>
            public bool IsAlive
            {
                get
                {
                    if (!_isSurelyIdle)
                    {
                        throw new InvalidOperationException(
                            "Cannot read this without ensuring that the instance is idle.");
                    }

                    return _isAlive;
                }
            }

            /// <summary>
            ///     If the server
            /// </summary>
            /// <exception cref="InvalidOperationException">
            ///     Thrown if you did not ensure that the instance is idle via <see cref="WaitIdleAsync"/> first.
            /// </exception>
            public Exception? UnhandledException
            {
                get
                {
                    if (!_isSurelyIdle)
                    {
                        throw new InvalidOperationException(
                            "Cannot read this without ensuring that the instance is idle.");
                    }

                    return _unhandledException;
                }
            }

            public List<string> TestsRan { get; } = new();

            private protected IntegrationInstance(IntegrationOptions? options)
            {
                Options = options;
                _testOut = TestContext.Out;

                var toInstance = Channel.CreateUnbounded<object>(new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = true
                });

                _toInstanceReader = toInstance.Reader;
                _toInstanceWriter = toInstance.Writer;

                var fromInstance = Channel.CreateUnbounded<object>(new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = true
                });

                _fromInstanceReader = fromInstance.Reader;
                _fromInstanceWriter = fromInstance.Writer;
            }

            /// <summary>
            ///     Resolve a dependency inside the instance.
            ///     This works identical to <see cref="IoCManager.Resolve{T}()"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException">
            ///     Thrown if you did not ensure that the instance is idle via <see cref="WaitIdleAsync"/> first.
            /// </exception>
            [Pure]
            public T ResolveDependency<T>()
            {
                if (!_isSurelyIdle)
                {
                    throw new InvalidOperationException(
                        "Cannot resolve services without ensuring that the instance is idle.");
                }

                return DependencyCollection.Resolve<T>();
            }

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
            public Task WaitIdleAsync(bool throwOnUnhandled = true, CancellationToken cancellationToken = default)
            {
                if (Options?.Asynchronous == true)
                {
                    return WaitIdleImplAsync(throwOnUnhandled, cancellationToken);
                }

                WaitIdleImplSync(throwOnUnhandled);
                return Task.CompletedTask;
            }

            private async Task WaitIdleImplAsync(bool throwOnUnhandled, CancellationToken cancellationToken)
            {
                while (_isAlive && _currentTicksId != _ackTicksId)
                {
                    object msg = default!;
                    try
                    {
                        msg = await _fromInstanceReader.ReadAsync(cancellationToken);
                    }
                    catch(OperationCanceledException ex)
                    {
                        _unhandledException = ex;
                        _isAlive = false;
                        break;
                    }
                    switch (msg)
                    {
                        case ShutDownMessage shutDownMessage:
                        {
                            _isAlive = false;
                            _isSurelyIdle = true;
                            _unhandledException = shutDownMessage.UnhandledException;
                            if (throwOnUnhandled && _unhandledException != null)
                            {
                                ExceptionDispatchInfo.Capture(_unhandledException).Throw();
                                return;
                            }

                            break;
                        }

                        case AckTicksMessage ack:
                        {
                            _ackTicksId = ack.MessageId;
                            break;
                        }

                        case AssertFailMessage assertFailMessage:
                        {
                            // Rethrow exception without losing stack trace.
                            ExceptionDispatchInfo.Capture(assertFailMessage.Exception).Throw();
                            break; // Unreachable.
                        }
                    }
                }

                _isSurelyIdle = true;
            }

            private void WaitIdleImplSync(bool throwOnUnhandled)
            {
                var oldSyncContext = SynchronizationContext.Current;
                try
                {
                    // Set up thread-local resources for instance switch.
                    {
                        var taskMgr = DependencyCollection.Resolve<TaskManager>();
                        IoCManager.InitThread(DependencyCollection, replaceExisting: true);
                        taskMgr.ResetSynchronizationContext();
                    }

                    GameLoop.SingleThreadRunUntilEmpty();
                    _isSurelyIdle = true;

                    while (_fromInstanceReader.TryRead(out var msg))
                    {
                        switch (msg)
                        {
                            case ShutDownMessage shutDownMessage:
                            {
                                _isAlive = false;
                                _unhandledException = shutDownMessage.UnhandledException;
                                if (throwOnUnhandled && _unhandledException != null)
                                {
                                    ExceptionDispatchInfo.Capture(_unhandledException).Throw();
                                    return;
                                }

                                break;
                            }

                            case AssertFailMessage assertFailMessage:
                            {
                                // Rethrow exception without losing stack trace.
                                ExceptionDispatchInfo.Capture(assertFailMessage.Exception).Throw();
                                break; // Unreachable.
                            }
                        }
                    }

                    _isSurelyIdle = true;
                }
                finally
                {
                    // NUnit has its own synchronization context so let's *not* break everything.
                    SynchronizationContext.SetSynchronizationContext(oldSyncContext);
                }
            }

            /// <summary>
            ///     Queue for the server to run n ticks.
            /// </summary>
            /// <param name="ticks">The amount of ticks to run.</param>
            public void RunTicks(int ticks)
            {
                _isSurelyIdle = false;
                _currentTicksId += 1;
                _toInstanceWriter.TryWrite(new RunTicksMessage(ticks, _currentTicksId));
            }

            /// <summary>
            ///     <see cref="RunTicks"/> followed by <see cref="WaitIdleAsync"/>
            /// </summary>
            public async Task WaitRunTicks(int ticks)
            {
                RunTicks(ticks);
                await WaitIdleAsync();
            }

            /// <summary>
            ///     Queue for the server to be stopped.
            /// </summary>
            public void Stop()
            {
                _isSurelyIdle = false;
                // Won't get ack'd directly but the shutdown is convincing enough.
                _currentTicksId += 1;
                _toInstanceWriter.TryWrite(new StopMessage());
                _toInstanceWriter.TryComplete();
            }

            /// <summary>
            ///     Queue for a delegate to be ran inside the main loop of the instance.
            /// </summary>
            /// <remarks>
            ///     Do not run NUnit assertions inside <see cref="Post"/>. Use <see cref="Assert"/> instead.
            /// </remarks>
            public void Post(Action post)
            {
                _isSurelyIdle = false;
                _currentTicksId += 1;
                _toInstanceWriter.TryWrite(new PostMessage(post, _currentTicksId));
            }

            public async Task WaitPost(Action post)
            {
                Post(post);
                await WaitIdleAsync();
            }

            /// <summary>
            ///     Queue for a delegate to be ran inside the main loop of the instance,
            ///     rethrowing any exceptions in <see cref="WaitIdleAsync"/>.
            /// </summary>
            /// <remarks>
            ///     Exceptions raised inside this callback will be rethrown by <see cref="WaitIdleAsync"/>.
            ///     This makes it ideal for NUnit assertions,
            ///     since rethrowing the NUnit assertion directly provides less noise.
            /// </remarks>
            public void Assert(Action assertion)
            {
                _isSurelyIdle = false;
                _currentTicksId += 1;
                _toInstanceWriter.TryWrite(new AssertMessage(assertion, _currentTicksId));
            }

            public async Task WaitAssertion(Action assertion)
            {
                Assert(assertion);
                await WaitIdleAsync();
            }

            public void Dispose()
            {
                Stop();
            }

            protected void LoadExtraPrototypes(IDependencyCollection deps, IntegrationOptions options)
            {
                var resMan = deps.Resolve<IResourceManagerInternal>();
                if (options.ExtraPrototypes != null)
                {
                    resMan.MountString("/Prototypes/__integration_extra.yml", options.ExtraPrototypes);
                }

                if (options.ExtraPrototypeList is {} list)
                {
                    for (var i = 0; i < list.Count; i++)
                    {
                        resMan.MountString($"/Prototypes/__integration_extra_{i}.yml", list[i]);
                    }
                }
            }
        }

        public sealed class ServerIntegrationInstance : IntegrationInstance
        {
            public ServerIntegrationInstance(ServerIntegrationOptions? options) : base(options)
            {
                ServerOptions = options;
                DependencyCollection = new DependencyCollection();
                if (options?.Asynchronous == true)
                {
                    InstanceThread = new Thread(_serverMain)
                    {
                        Name = "Server Instance Thread",
                        IsBackground = true
                    };
                    InstanceThread.Start();
                }
                else
                {
                    Init();
                }
            }

            public override IntegrationOptions? Options
            {
                get => ServerOptions;
                internal set => ServerOptions = (ServerIntegrationOptions?) value;
            }

            public ServerIntegrationOptions? ServerOptions { get; internal set; }

            public ServerIntegrationOptions? PreviousOptions { get; internal set; }

            private void _serverMain()
            {
                try
                {
                    var server = Init();
                    GameLoop.Run();
                    server.FinishMainLoop();
                    IoCManager.Clear();
                }
                catch (Exception e)
                {
                    _fromInstanceWriter.TryWrite(new ShutDownMessage(e));
                    return;
                }

                _fromInstanceWriter.TryWrite(new ShutDownMessage(null));
            }

            private BaseServer Init()
            {
                var deps = DependencyCollection;
                IoCManager.InitThread(deps, replaceExisting: true);
                ServerIoC.RegisterIoC(deps);
                deps.Register<INetManager, IntegrationNetManager>(true);
                deps.Register<IServerNetManager, IntegrationNetManager>(true);
                deps.Register<IntegrationNetManager, IntegrationNetManager>(true);
                deps.Register<ISystemConsoleManager, SystemConsoleManagerDummy>(true);
                deps.Register<IModLoader, TestingModLoader>(true);
                deps.Register<IModLoaderInternal, TestingModLoader>(true);
                deps.Register<TestingModLoader, TestingModLoader>(true);
                deps.RegisterInstance<IStatusHost>(new Mock<IStatusHost>().Object, true);
                deps.Register<IRobustMappedStringSerializer, IntegrationMappedStringSerializer>(true);
                deps.Register<IServerConsoleHost, TestingServerConsoleHost>(true);
                deps.Register<IConsoleHost, TestingServerConsoleHost>(true);
                deps.Register<IConsoleHostInternal, TestingServerConsoleHost>(true);
                Options?.InitIoC?.Invoke();
                deps.BuildGraph();
                //ServerProgram.SetupLogging();
                ServerProgram.InitReflectionManager(deps);
                deps.Resolve<IReflectionManager>().LoadAssemblies(typeof(RobustIntegrationTest).Assembly);

                var server = DependencyCollection.Resolve<BaseServer>();

                var serverOptions = ServerOptions?.Options ?? new ServerOptions()
                {
                    LoadConfigAndUserData = false,
                    LoadContentResources = false,
                };

                // Autoregister components if options are null or we're NOT starting from content, as in that case
                // components will get auto-registered later. But either way, we will still invoke
                // BeforeRegisterComponents here.
                Options?.BeforeRegisterComponents?.Invoke();
                if (!Options?.ContentStart ?? true)
                {
                    var componentFactory = deps.Resolve<IComponentFactory>();
                    componentFactory.DoAutoRegistrations();
                    componentFactory.GenerateNetIds();
                }

                if (Options?.ContentAssemblies != null)
                {
                    deps.Resolve<TestingModLoader>().Assemblies = Options.ContentAssemblies;
                }

                var cfg = deps.Resolve<IConfigurationManagerInternal>();

                cfg.LoadCVarsFromAssembly(typeof(RobustIntegrationTest).Assembly);

                if (Options != null)
                {
                    Options.BeforeStart?.Invoke();
                    cfg.OverrideConVars(Options.CVarOverrides.Select(p => (p.Key, p.Value)));
                    LoadExtraPrototypes(deps, Options);
                }

                cfg.OverrideConVars(new[]
                {
                    ("log.runtimelog", "false"),
                    (CVars.SysWinTickPeriod.Name, "-1"),
                    (CVars.SysGCCollectStart.Name, "false"),
                    (RTCVars.FailureLogLevel.Name, (Options?.FailureLogLevel ?? RTCVars.FailureLogLevel.DefaultValue).ToString()),

                    (CVars.ResCheckBadFileExtensions.Name, "false")
                });

                server.ContentStart = Options?.ContentStart ?? false;
                var logHandler = Options?.OverrideLogHandler ?? (() => new TestLogHandler(cfg, "SERVER", _testOut));
                if (server.Start(serverOptions, logHandler))
                {
                    throw new Exception("Server failed to start.");
                }

                GameLoop = new IntegrationGameLoop(
                    DependencyCollection.Resolve<IGameTiming>(),
                    _fromInstanceWriter, _toInstanceReader);
                server.OverrideMainLoop(GameLoop);
                server.SetupMainLoop();

                GameLoop.RunInit();
                ResolveIoC(deps);

                return server;
            }

            /// <summary>
            /// Force a PVS update. This is mainly here to expose internal PVS methods to content benchmarks.
            /// </summary>
            public void PvsTick(ICommonSession[] players)
            {
                var pvs = EntMan.System<PvsSystem>();
                pvs.SendGameStates(players);
                Timing.CurTick += 1;
            }

            /// <summary>
            /// Adds multiple dummy players to the server.
            /// </summary>
            public async Task<ICommonSession[]> AddDummySessions(int count)
            {
                var sessions = new ICommonSession[count];
                for (var i = 0; i < sessions.Length; i++)
                {
                    sessions[i] = await AddDummySession();
                }

                return sessions;
            }

            /// <summary>
            /// Adds a dummy player to the server.
            /// </summary>
            public async Task<ICommonSession> AddDummySession(string? userName = null)
            {
                userName ??= $"integration_dummy_{DummyUsers.Count}";
                Log.Info($"Adding dummy session {userName}");
                if (!_dummyUsers.TryGetValue(userName, out var userId))
                    _dummyUsers[userName] = userId = new(Guid.NewGuid());

                var man = (Robust.Server.Player.PlayerManager) PlayerMan;
                var session = man.AddDummySession(userId, userName);
                _dummySessions.Add(userId, session);

                session.ConnectedTime = DateTime.UtcNow;
                await WaitPost(() => man.SetStatus(session, SessionStatus.Connected));

                return session;
            }

            /// <summary>
            /// Removes a dummy player from the server.
            /// </summary>
            public async Task RemoveDummySession(ICommonSession session, bool removeUser = false)
            {
                Log.Info($"Removing dummy session {session.Name}");
                _dummySessions.Remove(session.UserId);
                var man = (Robust.Server.Player.PlayerManager) PlayerMan;
                await WaitPost(() => man.EndSession(session.UserId));
                if (removeUser)
                    _dummyUsers.Remove(session.Name);
            }

            /// <summary>
            /// Removes all dummy players from the server.
            /// </summary>
            public async Task RemoveAllDummySessions()
            {
                foreach (var session in _dummySessions.Values)
                {
                    await RemoveDummySession(session);
                }
            }

            private Dictionary<string, NetUserId> _dummyUsers = new();
            private Dictionary<NetUserId, ICommonSession> _dummySessions = new();
            public IReadOnlyDictionary<string, NetUserId> DummyUsers => _dummyUsers;
            public IReadOnlyDictionary<NetUserId, ICommonSession> DummySessions => _dummySessions;
        }

        public sealed class ClientIntegrationInstance : IntegrationInstance
        {
            [Obsolete("Use Session instead")]
            public LocalPlayer? Player => ((IPlayerManager) PlayerMan).LocalPlayer;
            public ICommonSession? Session => ((IPlayerManager) PlayerMan).LocalSession;
            public NetUserId? User => Session?.UserId;
            public EntityUid? AttachedEntity => Session?.AttachedEntity;

            public ClientIntegrationInstance(ClientIntegrationOptions? options) : base(options)
            {
                ClientOptions = options;
                DependencyCollection = new DependencyCollection();

                if (options?.Asynchronous == true)
                {
                    InstanceThread = new Thread(ThreadMain)
                    {
                        Name = "Client Instance Thread",
                        IsBackground = true
                    };
                    InstanceThread.Start();
                }
                else
                {
                    Init();
                }
            }

            public override IntegrationOptions? Options
            {
                get => ClientOptions;
                internal set => ClientOptions = (ClientIntegrationOptions?) value;
            }

            public ClientIntegrationOptions? ClientOptions { get; internal set; }

            public ClientIntegrationOptions? PreviousOptions { get; internal set; }

            /// <summary>
            ///     Wire up the server to connect to when <see cref="IClientNetManager.ClientConnect"/> gets called.
            /// </summary>
            public void SetConnectTarget(ServerIntegrationInstance server)
            {
                var clientNetManager = ResolveDependency<IntegrationNetManager>();
                var serverNetManager = server.ResolveDependency<IntegrationNetManager>();

                if (!serverNetManager.IsRunning)
                {
                    throw new InvalidOperationException("Server net manager is not running!");
                }

                clientNetManager.NextConnectChannel = serverNetManager.MessageChannelWriter;
            }

            public async Task CheckSandboxed(Assembly assembly)
            {
                await WaitIdleAsync();
                await WaitAssertion(() =>
                {
                    var modLoader = new ModLoader();
                    IoCManager.InjectDependencies(modLoader);
                    var cast = (IPostInjectInit) modLoader;
                    cast.PostInject();
                    modLoader.SetEnableSandboxing(true);
                    modLoader.LoadGameAssembly(assembly.Location);
                });
            }

            private void ThreadMain()
            {
                try
                {
                    var client = Init();
                    GameLoop.Run();
                    client.CleanupGameThread();
                    client.CleanupWindowThread();
                }
                catch (Exception e)
                {
                    _fromInstanceWriter.TryWrite(new ShutDownMessage(e));
                    return;
                }

                _fromInstanceWriter.TryWrite(new ShutDownMessage(null));
            }

            private GameController Init()
            {
                var deps = DependencyCollection;
                IoCManager.InitThread(deps, replaceExisting: true);
                ClientIoC.RegisterIoC(GameController.DisplayMode.Headless, deps);
                deps.Register<INetManager, IntegrationNetManager>(true);
                deps.Register<IClientNetManager, IntegrationNetManager>(true);
                deps.Register<IClientGameTiming, ClientGameTiming>(true);
                deps.Register<IntegrationNetManager, IntegrationNetManager>(true);
                deps.Register<IModLoader, TestingModLoader>(true);
                deps.Register<IModLoaderInternal, TestingModLoader>(true);
                deps.Register<TestingModLoader, TestingModLoader>(true);
                deps.Register<IRobustMappedStringSerializer, IntegrationMappedStringSerializer>(true);
                deps.Register<IClientConsoleHost, TestingClientConsoleHost>(true);
                deps.Register<IConsoleHost, TestingClientConsoleHost>(true);
                deps.Register<IConsoleHostInternal, TestingClientConsoleHost>(true);
                Options?.InitIoC?.Invoke();
                deps.BuildGraph();

                GameController.RegisterReflection(deps);
                deps.Resolve<IReflectionManager>().LoadAssemblies(typeof(RobustIntegrationTest).Assembly);

                var client = DependencyCollection.Resolve<GameController>();

                var clientOptions = ClientOptions?.Options ?? new GameControllerOptions()
                {
                    LoadContentResources = false,
                    LoadConfigAndUserData = false,
                };

                // Autoregister components if options are null or we're NOT starting from content, as in that case
                // components will get auto-registered later. But either way, we will still invoke
                // BeforeRegisterComponents here.
                Options?.BeforeRegisterComponents?.Invoke();
                if (!Options?.ContentStart ?? true)
                {
                    var componentFactory = deps.Resolve<IComponentFactory>();
                    componentFactory.DoAutoRegistrations();
                    componentFactory.GenerateNetIds();
                }

                if (Options?.ContentAssemblies != null)
                {
                    deps.Resolve<TestingModLoader>().Assemblies = Options.ContentAssemblies;
                }

                var cfg = deps.Resolve<IConfigurationManagerInternal>();

                cfg.LoadCVarsFromAssembly(typeof(RobustIntegrationTest).Assembly);

                if (Options != null)
                {
                    Options.BeforeStart?.Invoke();
                    cfg.OverrideConVars(Options.CVarOverrides.Select(p => (p.Key, p.Value)));
                    LoadExtraPrototypes(deps, Options);
                }

                cfg.OverrideConVars(new[]
                {
                    (CVars.NetPredictLagBias.Name, "0"),

                    // Connecting to Discord is a massive waste of time.
                    // Basically just makes the CI logs a mess.
                    (CVars.DiscordEnabled.Name, "false"),

                    // Avoid preloading textures.
                    (CVars.ResTexturePreloadingEnabled.Name, "false"),

                    (CVars.SysGCCollectStart.Name, "false"),

                    (RTCVars.FailureLogLevel.Name, (Options?.FailureLogLevel ?? RTCVars.FailureLogLevel.DefaultValue).ToString()),

                    (CVars.ResPrototypeReloadWatch.Name, "false"),

                    (CVars.ResCheckBadFileExtensions.Name, "false")
                });

                GameLoop = new IntegrationGameLoop(DependencyCollection.Resolve<IGameTiming>(),
                    _fromInstanceWriter, _toInstanceReader);

                client.OverrideMainLoop(GameLoop);
                client.ContentStart = Options?.ContentStart ?? false;
                client.StartupSystemSplash(
                    clientOptions,
                    Options?.OverrideLogHandler ?? (() => new TestLogHandler(cfg, "CLIENT", _testOut)),
                    globalExceptionLog: false);
                client.StartupContinue(GameController.DisplayMode.Headless);

                GameLoop.RunInit();
                ResolveIoC(deps);

                // Offset client generated Uids.
                // Not that we have client-server uid separation, there might be bugs where tests might accidentally
                // use server side uids on the client and vice versa. This can sometimes accidentally work if the
                // entities get created in the same order. For that reason we arbitrarily increment the queued Uid by
                // some arbitrary quantity.

                /* TODO: End my suffering and fix this because entmanager hasn't started up yet.
                for (var i = 0; i < 10; i++)
                {
                    EntMan.SpawnEntity(null, MapCoordinates.Nullspace);
                }
                */

                return client;
            }

            /// <summary>
            /// Directly pass a bound key event to a control.
            /// </summary>
            public async Task DoGuiEvent(Control control, GUIBoundKeyEventArgs args)
            {
                await WaitPost(() =>
                {
                    if (args.State == BoundKeyState.Down)
                        control.KeyBindDown(args);
                    else
                        control.KeyBindUp(args);
                });
            }
        }

        // Synchronization between the integration instance and the main loop is done purely through message passing.
        // The main thread sends commands like "run n ticks" and the main loop reports back the commands it has finished.
        // It also reports when it dies, of course.

        internal sealed class IntegrationGameLoop : IGameLoop
        {
            private readonly IGameTiming _gameTiming;

            private readonly ChannelWriter<object> _channelWriter;
            private readonly ChannelReader<object> _channelReader;

#pragma warning disable 67
            public event EventHandler<FrameEventArgs>? Input;
            public event EventHandler<FrameEventArgs>? Tick;
            public event EventHandler<FrameEventArgs>? Update;
            public event EventHandler<FrameEventArgs>? Render;
#pragma warning restore 67

            public bool SingleStep { get; set; }
            public bool Running { get; set; }
            public int MaxQueuedTicks { get; set; }
            public SleepMode SleepMode { get; set; }

            public IntegrationGameLoop(IGameTiming gameTiming, ChannelWriter<object> channelWriter,
                ChannelReader<object> channelReader)
            {
                _gameTiming = gameTiming;
                _channelWriter = channelWriter;
                _channelReader = channelReader;
            }

            public void Run()
            {
                // Main run method is only used when running from asynchronous thread.

                // Ack tick message 1 is implied as "init done"
                _channelWriter.TryWrite(new AckTicksMessage(1));

                while (Running)
                {
                    var readerNotDone = _channelReader.WaitToReadAsync().AsTask().GetAwaiter().GetResult();
                    if (!readerNotDone)
                    {
                        Running = false;
                        return;
                    }
                    SingleThreadRunUntilEmpty();
                }
            }

            public void RunInit()
            {
                Running = true;

                _gameTiming.InSimulation = true;
            }

            public void SingleThreadRunUntilEmpty()
            {
                while (Running && _channelReader.TryRead(out var message))
                {
                    switch (message)
                    {
                        case RunTicksMessage msg:
                            _gameTiming.InSimulation = true;
                            var simFrameEvent = new FrameEventArgs((float) _gameTiming.TickPeriod.TotalSeconds);
                            for (var i = 0; i < msg.Ticks && Running; i++)
                            {
                                Input?.Invoke(this, simFrameEvent);
                                Tick?.Invoke(this, simFrameEvent);
                                _gameTiming.CurTick = new GameTick(_gameTiming.CurTick.Value + 1);
                                Update?.Invoke(this, simFrameEvent);
                            }

                            _channelWriter.TryWrite(new AckTicksMessage(msg.MessageId));
                            break;

                        case StopMessage _:
                            Running = false;
                            break;

                        case PostMessage postMessage:
                            postMessage.Post();
                            _channelWriter.TryWrite(new AckTicksMessage(postMessage.MessageId));
                            break;

                        case AssertMessage assertMessage:
                            try
                            {
                                assertMessage.Assertion();
                            }
                            catch (Exception e)
                            {
                                _channelWriter.TryWrite(new AssertFailMessage(e));
                            }

                            _channelWriter.TryWrite(new AckTicksMessage(assertMessage.MessageId));
                            break;
                    }
                }
            }
        }

        [Virtual]
        public class ServerIntegrationOptions : IntegrationOptions
        {
            public virtual ServerOptions Options { get; set; } = new()
            {
                LoadConfigAndUserData = false,
                LoadContentResources = false,
            };
        }

        [Virtual]
        public class ClientIntegrationOptions : IntegrationOptions
        {
            public virtual GameControllerOptions Options { get; set; } = new()
            {
                LoadContentResources = false,
                LoadConfigAndUserData = false,
            };
        }

        public abstract class IntegrationOptions
        {
            public Action? InitIoC { get; set; }
            public Action? BeforeRegisterComponents { get; set; }
            public Action? BeforeStart { get; set; }
            public Assembly[]? ContentAssemblies { get; set; }

            /// <summary>
            /// String containing extra prototypes to load. Contents of the string are treated like a yaml file in the
            /// resources folder.
            /// </summary>
            public string? ExtraPrototypes { get; set; }

            /// <summary>
            /// List of strings containing extra prototypes to load. Contents of the strings are treated like yaml files
            /// in the resources folder.
            /// </summary>
            public List<string>? ExtraPrototypeList;

            public LogLevel? FailureLogLevel { get; set; } = RTCVars.FailureLogLevel.DefaultValue;
            public bool ContentStart { get; set; } = false;

            public Dictionary<string, string> CVarOverrides { get; } = new();
            public bool Asynchronous { get; set; } = true;
            public bool? Pool { get; set; }

            public Func<ILogHandler>? OverrideLogHandler { get; set; }
        }

        /// <summary>
        ///     Sent head -> instance to tell the instance to run a few simulation ticks.
        /// </summary>
        private sealed class RunTicksMessage
        {
            public RunTicksMessage(int ticks, int messageId)
            {
                Ticks = ticks;
                MessageId = messageId;
            }

            public int Ticks { get; }
            public int MessageId { get; }
        }

        /// <summary>
        ///     Sent head -> instance to tell the instance to shut down cleanly.
        /// </summary>
        private sealed class StopMessage
        {
        }

        /// <summary>
        ///     Sent instance -> head to confirm finishing of ticks message.
        /// </summary>
        private sealed class AckTicksMessage
        {
            public AckTicksMessage(int messageId)
            {
                MessageId = messageId;
            }

            public int MessageId { get; }
        }

        private sealed class AssertFailMessage
        {
            public Exception Exception { get; }

            public AssertFailMessage(Exception exception)
            {
                Exception = exception;
            }
        }

        /// <summary>
        ///     Sent instance -> head when instance shuts down for whatever reason.
        /// </summary>
        private sealed class ShutDownMessage
        {
            public ShutDownMessage(Exception? unhandledException)
            {
                UnhandledException = unhandledException;
            }

            public Exception? UnhandledException { get; }
        }

        private sealed class PostMessage
        {
            public Action Post { get; }
            public int MessageId { get; }

            public PostMessage(Action post, int messageId)
            {
                Post = post;
                MessageId = messageId;
            }
        }

        private sealed class AssertMessage
        {
            public Action Assertion { get; }
            public int MessageId { get; }

            public AssertMessage(Action assertion, int messageId)
            {
                Assertion = assertion;
                MessageId = messageId;
            }
        }
    }
}
