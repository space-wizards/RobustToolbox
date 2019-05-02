using System;
using System.Collections.Generic;
using System.Threading;
using Robust.Server.Interfaces;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using ServerEntryPoint = Robust.Server.EntryPoint;

namespace Robust.UnitTesting
{
    public abstract class RobustIntegrationTest
    {
        private Thread ServerThread;
        private IntegrationGameLoop ServerGameLoop;
        private Thread ClientThread;

        protected bool ServerIsRunning => ServerThread?.IsAlive ?? false;
        protected bool ClientIsRunning => ClientThread?.IsAlive ?? false;

        protected void ServerStart()
        {
            ServerThread = new Thread(_serverMain);
            ServerThread.Start();
        }

        protected void StartClient()
        {
            //ClientThread = new Thread(() =>
            //{
            //    Robust.Client.GameController.Main();
            //});
        }

        private void _serverMain()
        {
            try
            {
                IoCManager.InitThread();
                ServerEntryPoint.RegisterIoC();
                IoCManager.BuildGraph();
                ServerEntryPoint.SetupLogging();
                ServerEntryPoint.InitReflectionManager();

                var server = IoCManager.Resolve<IBaseServerInternal>();

                if (server.Start())
                {
                    // TODO: Store load fail.
                    Console.WriteLine("Server failed to start.");
                    return;
                }

                ServerGameLoop = new IntegrationGameLoop(IoCManager.Resolve<IGameTiming>());
                server.OverrideMainLoop(ServerGameLoop);

                server.MainLoop();
            }
            catch (Exception e)
            {
                Console.WriteLine("Server crashed on exception: {0}", e);
                // TODO: Store exception somewhere.
            }
        }

        protected void ServerRunTicks(int ticks = 1)
        {
            ServerGameLoop?.PushTicks(ticks);
        }

        protected void ServerWaitIdle()
        {
            ServerGameLoop?.WaitDone();
        }

        private sealed class IntegrationGameLoop : IGameLoop
        {
            private readonly IGameTiming _gameTiming;

            public event EventHandler<FrameEventArgs> Input;
            public event EventHandler<FrameEventArgs> Tick;
            public event EventHandler<FrameEventArgs> Update;
            public event EventHandler<FrameEventArgs> Render;

            public bool SingleStep { get; set; }
            public bool Running { get; set; }
            public int MaxQueuedTicks { get; set; }
            public SleepMode SleepMode { get; set; }

            private readonly ManualResetEventSlim _resumeEvent = new ManualResetEventSlim();
            private readonly ManualResetEventSlim _queueDoneEvent = new ManualResetEventSlim();
            private readonly Queue<object> _messageChannel = new Queue<object>();

            public IntegrationGameLoop(IGameTiming gameTiming)
            {
                _gameTiming = gameTiming;
            }

            public void Run()
            {
                Tick += (a, b) => Console.WriteLine("tick");

                var simFrameEvent = new MutableFrameEventArgs(0);
                _gameTiming.InSimulation = true;

                while (Running)
                {
                    _resumeEvent.Wait();

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
                    }
                }
            }

            public void PushTicks(int ticks)
            {
                lock (_messageChannel)
                {
                    _messageChannel.Enqueue(new RunTicksMessage(ticks, 1/60f));
                    _resumeEvent.Set();
                }
            }

            public void WaitDone()
            {
                _queueDoneEvent.Wait();
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
        }
    }
}
