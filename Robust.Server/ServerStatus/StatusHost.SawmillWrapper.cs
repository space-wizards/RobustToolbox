using System;
using Microsoft.Extensions.Logging;
using Robust.Shared.Interfaces.Log;

namespace Robust.Server.ServerStatus
{

    internal sealed partial class StatusHost
    {

        private class SawmillWrapper : ILogger
        {

            private ISawmill _sawmill;

            public SawmillWrapper(ISawmill sawmill)
                => _sawmill = sawmill;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
                => _sawmill.Log((Shared.Log.LogLevel) (int) logLevel, formatter(state, exception));

            public bool IsEnabled(LogLevel logLevel)
                => (int) logLevel >= (int) (_sawmill.Level ?? (Shared.Log.LogLevel) 0);

            public IDisposable BeginScope<TState>(TState state)
                => new DummyDisposable();

            // @formatter:off
            private struct DummyDisposable : IDisposable { public void Dispose() { } }
            // @formatter:on

        }

    }

}
