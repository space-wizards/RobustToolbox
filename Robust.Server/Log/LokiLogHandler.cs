using System;
using System.Collections.Generic;
using Robust.Shared.Log;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Loki;
using Serilog.Sinks.Loki.Labels;
using SLogger = Serilog.Core.Logger;

namespace Robust.Server.Log
{
    public sealed class LokiLogHandler : ILogHandler, IDisposable
    {
        private readonly SLogger _sLogger;

        public LokiLogHandler(LokiSinkConfiguration configuration)
        {
            _sLogger = new LoggerConfiguration()
                .WriteTo.LokiHttp(() => configuration)
                .MinimumLevel.Debug()
                .CreateLogger();
        }

        public void Log(string sawmillName, LogEvent message)
        {
            var valid = _sLogger.BindProperty(LogManager.SawmillProperty, sawmillName, false, out var sawmillProperty);

            if (valid)
            {
                message.AddOrUpdateProperty(sawmillProperty);
            }

            _sLogger.Write(message);
        }

        public void Dispose()
        {
            _sLogger.Dispose();
        }
    }

    public sealed class LogLabelProvider : ILogLabelProvider
    {
        private readonly string _serverName;

        public LogLabelProvider(string serverName)
        {
            _serverName = serverName;
        }

        public IList<LokiLabel> GetLabels()
        {
            return new[]
            {
                new LokiLabel("App", "Robust.Server"),
                new LokiLabel("Server", _serverName),
            };
        }

        public IList<string> PropertiesAsLabels => new[] {"level"};
        public IList<string> PropertiesToAppend => Array.Empty<string>();
        public LokiFormatterStrategy FormatterStrategy => LokiFormatterStrategy.SpecificPropertiesAsLabelsAndRestAppended;
    }
}
