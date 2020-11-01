using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using Robust.Shared.Asynchronous;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Robust.Shared
{
    internal abstract class SignalHandler : ISignalHandler, IDisposable
    {
        [Dependency] private readonly ITaskManager _taskManager = default!;
#pragma warning disable 414
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
#pragma warning restore 414

        private Thread? _signalThread;

        public void MaybeStart()
        {
            // I actually did try to implement a onValueChanged handler but couldn't make it work well.
            // The problem is that shutting down the thread does not restore the default exit behavior.
#if UNIX
            if (_configurationManager.GetCVar(CVars.SignalsHandle))
            {
                Start();
            }
#endif
        }

        [SuppressMessage("ReSharper", "IdentifierTypo")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "CommentTypo")]
        private void Start()
        {
            var runningOnMono = Type.GetType("Mono.Runtime") != null;
            if (!runningOnMono)
            {
                return;
            }

            try
            {
                // Reflection is fun.
                var assembly = FindMonoPosix();
                Logger.Debug("Successfully loaded Mono.Posix. Registering signal handlers...");

                var signalType = assembly.GetType("Mono.Unix.UnixSignal")!;
                // Mono.Unix.UnixSignal[]
                var signalArrayType = signalType.MakeArrayType();
                var signumType = assembly.GetType("Mono.Unix.Native.Signum")!;

                var SIGTERM = Enum.Parse(signumType, "SIGTERM");
                var SIGINT = Enum.Parse(signumType, "SIGINT");

                // int UnixSignal.WaitAny(UnixSignal[])
                var WaitAny = signalType.GetMethod("WaitAny", new Type[] {signalArrayType})!;
                // UnixSignal.Signum
                var Signum = signalType.GetProperty("Signum")!;

                var signals = Array.CreateInstance(signalType, 2);
                signals.SetValue(Activator.CreateInstance(signalType, SIGTERM), 0);
                signals.SetValue(Activator.CreateInstance(signalType, SIGINT), 1);

                _signalThread = new Thread(() =>
                {
                    while (true)
                    {
                        var args = new object[] {signals};
                        // int UnixSignal.WaitAny(UnixSignal[])
                        // ReSharper disable once PossibleNullReferenceException
                        var index = (int) WaitAny.Invoke(null, args)!;
                        // signals[index].Signum
                        // ReSharper disable once PossibleNullReferenceException
                        var signum = Signum.GetValue(signals.GetValue(index), null)!.ToString();

                        // Can't use switch with reflection. Shame.
                        // Tried to compare the objects directly. Didn't work.
                        // String it is.
                        if (signum == "SIGINT" || signum == "SIGTERM")
                        {
                            _taskManager.RunOnMainThread(() => OnReceiveTerminationSignal(signum));
                        }
                    }

                    // ReSharper disable once FunctionNeverReturns
                })
                {
                    IsBackground = true,
                    Name = "signal handler"
                };

                _signalThread.Start();
            }
            catch (Exception e)
            {
                Logger.Error("Running on mono but couldn't register signal handlers: {0}", e);
            }
        }

        private void Stop()
        {
            _signalThread?.Abort();
            _signalThread = null;
        }

        protected abstract void OnReceiveTerminationSignal(string signal);

        private static Assembly FindMonoPosix()
        {
            // This works don't touch it.
            // Well it works on MacOS. Can't speak about Linux.
            return Assembly.Load("../Mono.Posix.dll");
        }

        public void Dispose()
        {
            Stop();
        }
    }

    internal interface ISignalHandler
    {
        void MaybeStart();
    }
}
