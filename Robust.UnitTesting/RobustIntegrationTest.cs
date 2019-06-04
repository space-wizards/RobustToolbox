using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Robust.Client;
using Robust.Client.Interfaces;
using Robust.Server.Interfaces;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using FrameEventArgs = Robust.Shared.Timing.FrameEventArgs;
using ServerEntryPoint = Robust.Server.EntryPoint;

namespace Robust.UnitTesting
{
    /// <summary>
    ///     Base class allowing you to implement integration tests.
    /// </summary>
    /// <remarks>
    ///     Integration tests allow you to act upon a running server as a whole,
    ///     contrary to unit testing which tests, well, units.
    /// </remarks>
    public abstract class RobustIntegrationTest
    {
        /// <summary>
        ///     Start an instance of the server and return an object that can be used to control it.
        /// </summary>
        protected static ServerIntegrationInstance StartServer()
        {
            return new ServerIntegrationInstance();
        }

        /// <summary>
        ///     Start a headless instance of the client and return an object that can be used to control it.
        /// </summary>
        protected static ClientIntegrationInstance StartClient()
        {
            return new ClientIntegrationInstance();
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
        public abstract class IntegrationInstance
        {
            private protected Thread InstanceThread;
            private protected IDependencyCollection DependencyCollection;

            private protected readonly Channel _toInstanceChannel = new Channel();
            private protected readonly Channel _fromInstanceChannel = new Channel();

            private int _currentTicksId = 1;
            private int _ackTicksId;

            private bool _isSurelyIdle;
            private bool _isAlive = true;
            private Exception _unhandledException;

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
            public Exception UnhandledException
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

            private protected IntegrationInstance()
            {
            }

            /// <summary>
            ///     Resolve a dependency inside the instance.
            ///     This works identical to <see cref="IoCManager.Resolve{T}"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException">
            ///     Thrown if you did not ensure that the instance is idle via <see cref="WaitIdleAsync"/> first.
            /// </exception>
            public T ResolveDependency<T>()
            {
                if (!_isSurelyIdle)
                {
                    throw new InvalidOperationException(
                        "Cannot resolve services without ensuring that the instance is idle.");
                }

                // TODO: Synchronize and ensure idle.
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
            public async Task WaitIdleAsync(bool throwOnUnhandled = true, CancellationToken cancellationToken = default)
            {
                try
                {
                    while (_isAlive && _currentTicksId != _ackTicksId)
                    {
                        var msg = await _fromInstanceChannel.WaitMessageAsync(cancellationToken);
                        switch (msg)
                        {
                            case ShutDownMessage shutDownMessage:
                            {
                                _isAlive = false;
                                _unhandledException = shutDownMessage.UnhandledException;
                                if (throwOnUnhandled && _unhandledException != null)
                                {
                                    throw new Exception("Waiting instance shut down with unhandled exception",
                                        _unhandledException);
                                }

                                break;
                            }

                            case AckTicksMessage ack:
                            {
                                _ackTicksId = ack.MessageId;
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    _isSurelyIdle = true;
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
                _toInstanceChannel.PushMessage(new RunTicksMessage(ticks, 1 / 60f, _currentTicksId));
            }

            /// <summary>
            ///     Queue for the server to be stopped.
            /// </summary>
            public void Stop()
            {
                _isSurelyIdle = false;
                // Won't get ack'd directly but the shutdown is convincing enough.
                _currentTicksId += 1;
                _toInstanceChannel.PushMessage(new StopMessage());
            }

            /// <summary>
            ///     Queue for a delegate to be ran inside the main loop of the instance.
            /// </summary>
            public void Post(Action post)
            {
                _isSurelyIdle = false;
                _currentTicksId += 1;
                _toInstanceChannel.PushMessage(new PostMessage(post, _currentTicksId));
            }
        }

        public sealed class ServerIntegrationInstance : IntegrationInstance
        {
            internal ServerIntegrationInstance()
            {
                InstanceThread = new Thread(_serverMain) {Name = "Server Instance Thread"};
                DependencyCollection = new DependencyCollection();
                InstanceThread.Start();
            }

            private void _serverMain()
            {
                try
                {
                    IoCManager.InitThread(DependencyCollection);
                    ServerEntryPoint.RegisterIoC();
                    IoCManager.BuildGraph();
                    ServerEntryPoint.SetupLogging();
                    ServerEntryPoint.InitReflectionManager();

                    var server = DependencyCollection.Resolve<IBaseServerInternal>();

                    server.ContentRootDir = "../../";

                    if (server.Start())
                    {
                        throw new Exception("Server failed to start.");
                    }

                    var gameLoop = new IntegrationGameLoop(
                        DependencyCollection.Resolve<IGameTiming>(),
                        _toInstanceChannel, _fromInstanceChannel);
                    server.OverrideMainLoop(gameLoop);

                    server.MainLoop();
                }
                catch (Exception e)
                {
                    _fromInstanceChannel.PushMessage(new ShutDownMessage(e));
                    return;
                }

                _fromInstanceChannel.PushMessage(new ShutDownMessage(null));
            }
        }

        public sealed class ClientIntegrationInstance : IntegrationInstance
        {
            internal ClientIntegrationInstance()
            {
                InstanceThread = new Thread(_clientMain) {Name = "Client Instance Thread"};
                DependencyCollection = new DependencyCollection();
                InstanceThread.Start();
            }

            private void _clientMain()
            {
                try
                {
                    IoCManager.InitThread(DependencyCollection);
                    GameController.RegisterIoC(GameController.DisplayMode.Headless);
                    IoCManager.BuildGraph();

                    GameController.RegisterReflection();

                    var client = DependencyCollection.Resolve<IGameControllerInternal>();

                    client.ContentRootDir = "../../";

                    client.Startup();

                    var gameLoop = new IntegrationGameLoop(DependencyCollection.Resolve<IGameTiming>(),
                        _toInstanceChannel, _fromInstanceChannel);
                    client.OverrideMainLoop(gameLoop);
                    client.MainLoop(GameController.DisplayMode.Headless);
                }
                catch (Exception e)
                {
                    _fromInstanceChannel.PushMessage(new ShutDownMessage(e));
                    return;
                }

                _fromInstanceChannel.PushMessage(new ShutDownMessage(null));
            }
        }

        // Synchronization between the integration instance and the main loop is done purely through message passing.
        // The main thread sends commands like "run n ticks" and the main loop reports back the commands it has finished.
        // It also reports when it dies, of course.

        internal sealed class IntegrationGameLoop : IGameLoop
        {
            private readonly IGameTiming _gameTiming;
            private readonly Channel _toInstanceChannel;
            private readonly Channel _fromInstanceChannel;

#pragma warning disable 67
            public event EventHandler<FrameEventArgs> Input;
            public event EventHandler<FrameEventArgs> Tick;
            public event EventHandler<FrameEventArgs> Update;
            public event EventHandler<FrameEventArgs> Render;
#pragma warning restore 67

            public bool SingleStep { get; set; }
            public bool Running { get; set; }
            public int MaxQueuedTicks { get; set; }
            public SleepMode SleepMode { get; set; }

            public IntegrationGameLoop(IGameTiming gameTiming, Channel toInstanceChannel, Channel fromInstanceChannel)
            {
                _gameTiming = gameTiming;
                _toInstanceChannel = toInstanceChannel;
                _fromInstanceChannel = fromInstanceChannel;
            }

            public void Run()
            {
                // Ack tick message 1 is implied as "init done"
                _fromInstanceChannel.PushMessage(new AckTicksMessage(1));
                Running = true;

                Tick += (a, b) => Console.WriteLine("tick: {0}", _gameTiming.CurTick);

                var simFrameEvent = new MutableFrameEventArgs(0);
                _gameTiming.InSimulation = true;

                while (Running)
                {
                    var message = _toInstanceChannel.WaitMessage();

                    switch (message)
                    {
                        case RunTicksMessage msg:
                            _gameTiming.InSimulation = true;
                            simFrameEvent.SetDeltaSeconds(msg.Delta);
                            for (var i = 0; i < msg.Ticks && Running; i++)
                            {
                                _gameTiming.CurTick = new GameTick(_gameTiming.CurTick.Value + 1);
                                Tick?.Invoke(this, simFrameEvent);
                            }

                            _fromInstanceChannel.PushMessage(new AckTicksMessage(msg.MessageId));
                            break;

                        case StopMessage _:
                            Running = false;
                            break;

                        case PostMessage postMessage:
                            postMessage.Post();
                            _fromInstanceChannel.PushMessage(new AckTicksMessage(postMessage.MessageId));
                            break;
                    }
                }
            }
        }

        internal sealed class Channel
        {
            private readonly object _lock = new object();
            private readonly Queue<object> _messageQueue = new Queue<object>();
            private readonly ManualResetEvent _resumeEvent = new ManualResetEvent(false);

            public void PushMessage(object message)
            {
                lock (_lock)
                {
                    _messageQueue.Enqueue(message);
                    _resumeEvent.Set();
                }
            }

            public object WaitMessage()
            {
                _resumeEvent.WaitOne();
                lock (_lock)
                {
                    var message = _messageQueue.Dequeue();
                    if (_messageQueue.Count == 0)
                    {
                        _resumeEvent.Reset();
                    }

                    return message;
                }
            }

            public async Task<object> WaitMessageAsync(CancellationToken cancellationToken)
            {
                await _resumeEvent.WaitOneAsync(cancellationToken);

                lock (_lock)
                {
                    var message = _messageQueue.Dequeue();
                    if (_messageQueue.Count == 0)
                    {
                        _resumeEvent.Reset();
                    }

                    return message;
                }
            }
        }

        /// <summary>
        ///     Sent head -> instance to tell the instance to run a few simulation ticks.
        /// </summary>
        private sealed class RunTicksMessage
        {
            public RunTicksMessage(int ticks, float delta, int messageId)
            {
                Ticks = ticks;
                Delta = delta;
                MessageId = messageId;
            }

            public int Ticks { get; }
            public float Delta { get; }
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

        /// <summary>
        ///     Sent instance -> head when instance shuts down for whatever reason.
        /// </summary>
        private sealed class ShutDownMessage
        {
            public ShutDownMessage(Exception unhandledException)
            {
                UnhandledException = unhandledException;
            }

            public Exception UnhandledException { get; }
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
    }
}
