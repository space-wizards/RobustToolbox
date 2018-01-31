using SS14.Server.Interfaces;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using System;
using System.Reflection;
using System.Threading;

// TODO: thread safety.
namespace SS14.Server
{
    static class SignalHander
    {
        static Thread SignalThread;

        public static void InstallSignals()
        {
            bool runningOnMono = Type.GetType ("Mono.Runtime") != null;
            if (!runningOnMono)
            {
                return;
            }
            try
            {
                // Reflection is fun.
                var assembly = FindMonoPosix();
                Logger.Log("Successfully loaded Mono.Posix. Registering signal handlers...", LogLevel.Debug);

                Type signalType = assembly.GetType("Mono.Unix.UnixSignal");
                // Mono.Unix.UnixSignal[]
                Type signalArrayType = signalType.MakeArrayType();
                Type signumType = assembly.GetType("Mono.Unix.Native.Signum");

                var SIGTERM = Enum.Parse(signumType, "SIGTERM");
                var SIGINT = Enum.Parse(signumType, "SIGINT");

                // int UnixSignal.WaitAny(UnixSignal[])
                var WaitAny = signalType.GetMethod("WaitAny", new Type[] { signalArrayType });
                // UnixSignal.Signum
                var Signum = signalType.GetProperty("Signum");

                Array signals = Array.CreateInstance(signalType, 2);
                signals.SetValue(Activator.CreateInstance(signalType, SIGTERM), 0);
                signals.SetValue(Activator.CreateInstance(signalType, SIGINT), 1);

                SignalThread = new Thread(() =>
                {
                    while (true)
                    {
                        object[] args = new object[] {
                                signals
                        };
                        // int UnixSignal.WaitAny(UnixSignal[])
                        int index = (int)WaitAny.Invoke(null, args);
                        // signals[index].Signum
                        string signum = Signum.GetValue(signals.GetValue(index), null).ToString();

                        // Can't use switch with reflection. Shame.
                        // Tried to compare the objects directly. Didn't work.
                        // String it is.
                        if (signum == "SIGINT" || signum == "SIGTERM")
                        {
                            IoCManager.Resolve<IBaseServer>().Shutdown(string.Format("{0} received", signum));
                        }
                    }
                });

                SignalThread.IsBackground = true;
                SignalThread.Name = "signal handler";
                SignalThread.Start();

            }
            catch (Exception e)
            {
                Logger.Log(string.Format("Running on mono but couldn't register signal handlers: {0}", e), LogLevel.Error);
            }
        }

        private static Assembly FindMonoPosix()
        {
            // This works don't touch it.
            // Well it works on MacOS. Can't speak about Linux.
            return Assembly.Load("../Mono.Posix.dll");
        }
    }
}
