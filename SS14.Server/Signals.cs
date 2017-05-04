using Mono.Unix;
using Mono.Unix.Native;
using SS14.Shared.IoC;
using SS14.Server.Interfaces;
using System;
using System.Threading;

// TODO: thread safety.
namespace SS14.Server
{
    static class SignalHander
    {
        static Thread SignalThread;

        public static void InstallSignals()
        {
            UnixSignal[] signals = new UnixSignal[] {
                new UnixSignal (Mono.Unix.Native.Signum.SIGTERM),
                new UnixSignal (Mono.Unix.Native.Signum.SIGINT)
            };

            SignalThread = new Thread(() =>
            {
                while (true)
                {
                    int index = UnixSignal.WaitAny(signals, -1);
                    Signum signum = signals[index].Signum;
                    switch (signum)
                    {
                        case Signum.SIGTERM:
                        case Signum.SIGINT:
                            IoCManager.Resolve<ISS14Server>().Shutdown(string.Format("{0} received", signum.ToString()));
                            break;
                    }
                }
            });

            SignalThread.IsBackground = true;
            SignalThread.Name = "signal handler"; 
            SignalThread.Start();
        }
    }
}
