using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Robust.Server.Interfaces;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using ServerEntryPoint = Robust.Server.EntryPoint;

namespace Robust.UnitTesting
{
    public abstract class RobustIntegrationTest
    {
        protected static ServerIntegrationInstance StartServer()
        {
            return new ServerIntegrationInstance();
        }

        public abstract class IntegrationInstance
        {
            protected Thread InstanceThread;
            protected IDependencyCollection DependencyCollection;
            private protected IntegrationGameLoop GameLoop;

            protected readonly object DependencyLock = new object();

            public bool IsAlive => InstanceThread?.IsAlive ?? false;

            protected readonly ManualResetEvent InitDoneEvent = new ManualResetEvent(false);
            protected readonly ManualResetEvent ShutdownEvent = new ManualResetEvent(false);
            private bool _shutdownRequested;

            private protected IntegrationInstance()
            {
            }

            public T ResolveDependency<T>()
            {
                lock (DependencyLock)
                {
                    return DependencyCollection.Resolve<T>();
                }
            }

            public async Task WaitIdleAsync(CancellationToken cancellationToken = default)
            {
                await InitDoneEvent.WaitOneAsync(cancellationToken);

                if (GameLoop != null)
                {
                    await GameLoop.WaitIdleAsync(cancellationToken);
                }

                if (_shutdownRequested)
                {
                    await ShutdownEvent.WaitOneAsync(cancellationToken);
                }
            }

            public void RunTicks(int ticks)
            {
                if (GameLoop == null)
                {
                    InitDoneEvent.WaitOne();
                }

                if (GameLoop == null)
                {
                    throw new InvalidOperationException("Server failed to start, GameLoop does not exist.");
                }

                GameLoop.PushTicks(ticks);
            }

            public void Stop()
            {
                if (GameLoop == null)
                {
                    InitDoneEvent.WaitOne();
                }

                if (GameLoop == null)
                {
                    throw new InvalidOperationException("Server failed to start, GameLoop does not exist.");
                }

                GameLoop.PushStop();
                _shutdownRequested = true;
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
                    IBaseServerInternal server;
                    lock (DependencyLock)
                    {
                        IoCManager.InitThread(DependencyCollection);
                        ServerEntryPoint.RegisterIoC();
                        IoCManager.BuildGraph();
                        ServerEntryPoint.SetupLogging();
                        ServerEntryPoint.InitReflectionManager();

                        server = DependencyCollection.Resolve<IBaseServerInternal>();

                        if (server.Start())
                        {
                            // TODO: Store load fail.
                            Console.WriteLine("Server failed to start.");
                            return;
                        }

                        GameLoop = new IntegrationGameLoop(DependencyCollection.Resolve<IGameTiming>());
                        server.OverrideMainLoop(GameLoop);
                    }

                    InitDoneEvent.Set();
                    server.MainLoop();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Server crashed on exception: {0}", e);
                    // TODO: Store exception somewhere.
                }
                finally
                {
                    InitDoneEvent.Set();
                    ShutdownEvent.Set();
                }
            }
        }

        internal sealed class IntegrationGameLoop : IGameLoop
        {
            private readonly IGameTiming _gameTiming;

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

            private readonly ManualResetEvent _resumeEvent = new ManualResetEvent(false);
            private readonly ManualResetEvent _queueDoneEvent = new ManualResetEvent(true);
            private readonly Queue<object> _messageChannel = new Queue<object>();

            public IntegrationGameLoop(IGameTiming gameTiming)
            {
                _gameTiming = gameTiming;
            }

            public void Run()
            {
                Running = true;

                try
                {
                    Tick += (a, b) => Console.WriteLine("tick: {0}", _gameTiming.CurTick);

                    var simFrameEvent = new MutableFrameEventArgs(0);
                    _gameTiming.InSimulation = true;

                    while (Running)
                    {
                        _resumeEvent.WaitOne();

                        object message;
                        lock (_messageChannel)
                        {
                            if (_messageChannel.Count == 0)
                            {
                                _queueDoneEvent.Set();
                                _resumeEvent.Reset();
                                continue;
                            }

                            message = _messageChannel.Dequeue();
                        }

                        _queueDoneEvent.Reset();

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

                                break;

                            case StopMessage msg:
                                Running = false;
                                break;
                        }
                    }
                }
                finally
                {
                    // Set queue done always so that if the main loop dies from an exception we don't deadlock.
                    _queueDoneEvent.Set();
                }
            }

            public void PushTicks(int ticks)
            {
                _pushMessage(new RunTicksMessage(ticks, 1 / 60f));
            }

            public void PushStop()
            {
                _pushMessage(new StopMessage());
            }

            private void _pushMessage(object message)
            {
                lock (_messageChannel)
                {
                    _messageChannel.Enqueue(message);
                    _resumeEvent.Set();
                    _queueDoneEvent.Reset();
                }
            }

            public Task WaitIdleAsync(CancellationToken cancellationToken = default)
            {
                return _queueDoneEvent.WaitOneAsync(cancellationToken);
            }

            private sealed class RunTicksMessage
            {
                public RunTicksMessage(int ticks, float delta)
                {
                    Ticks = ticks;
                    Delta = delta;
                }

                public int Ticks { get; }
                public float Delta { get; }
            }

            private sealed class StopMessage
            {
            }
        }
    }
}
