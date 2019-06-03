using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Robust.Client;
using Robust.Client.Interfaces;
using Robust.Server.Interfaces;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using FrameEventArgs = Robust.Shared.Timing.FrameEventArgs;
using ServerEntryPoint = Robust.Server.EntryPoint;

namespace Robust.UnitTesting
{
    public abstract class RobustIntegrationTest
    {
        protected static ServerIntegrationInstance StartServer()
        {
            return new ServerIntegrationInstance();
        }

        protected static ClientIntegrationInstance StartClient()
        {
            return new ClientIntegrationInstance();
        }

        public abstract class IntegrationInstance
        {
            private protected Thread InstanceThread;
            private protected IDependencyCollection DependencyCollection;

            private protected readonly Channel _toInstanceChannel = new Channel();
            private protected readonly Channel _fromInstanceChannel = new Channel();

            private int _currentTicksId = 1;
            private int _ackTicksId;

            public bool IsAlive { get; private set; } = true;
            public Exception UnhandledException { get; private set; }

            private protected IntegrationInstance()
            {
            }

            public T ResolveDependency<T>()
            {
                // TODO: Synchronize and ensure idle.
                return DependencyCollection.Resolve<T>();
            }

            public async Task WaitIdleAsync(CancellationToken cancellationToken = default)
            {
                while (IsAlive && _currentTicksId != _ackTicksId)
                {
                    var msg = await _fromInstanceChannel.WaitMessageAsync(cancellationToken);
                    switch (msg)
                    {
                        case ShutDownMessage shutDownMessage:
                        {
                            IsAlive = false;
                            UnhandledException = shutDownMessage.UnhandledException;
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

            public void RunTicks(int ticks)
            {
                _currentTicksId += 1;
                _toInstanceChannel.PushMessage(new RunTicksMessage(ticks, 1 / 60f, _currentTicksId));
            }

            public void Stop()
            {
                // Won't get ack'd directly but the shutdown is convincing enough.
                _currentTicksId += 1;
                _toInstanceChannel.PushMessage(new StopMessage());
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
                    Console.WriteLine("Server crashed on exception: {0}", e);
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

                    var gameLoop = new IntegrationGameLoop(DependencyCollection.Resolve<IGameTiming>(), _toInstanceChannel, _fromInstanceChannel);
                    client.OverrideMainLoop(gameLoop);
                    client.MainLoop(GameController.DisplayMode.Headless);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Client crashed on exception: {0}", e);
                    _fromInstanceChannel.PushMessage(new ShutDownMessage(e));
                    return;
                }

                _fromInstanceChannel.PushMessage(new ShutDownMessage(null));
            }
        }

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
    }
}
