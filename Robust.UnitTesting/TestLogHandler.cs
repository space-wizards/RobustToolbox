using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Serilog.Events;

namespace Robust.UnitTesting
{
    public sealed class TestLogHandler : ILogHandler, IDisposable
    {
        private readonly string? _prefix;
        private readonly TextWriter _writer;
        private readonly Stopwatch _sw = Stopwatch.StartNew();

        public TestLogHandler(IConfigurationManager cfg, string? prefix = null)
        {
            cfg.OnValueChanged(RTCVars.FailureLogLevel, value => FailureLevel = value, true);

            _prefix = prefix;
            _writer = TestContext.Out;
            _writer.WriteLine($"{GetPrefix()}Started {DateTime.Now:o}");
        }

        private LogLevel? FailureLevel { get; set; }

        public void Dispose()
        {
            _writer.Dispose();
        }

        public void Log(string sawmillName, LogEvent message)
        {
            var level = message.Level.ToRobust();
            var name = LogMessage.LogLevelToName(level);
            var seconds = _sw.ElapsedMilliseconds / 1000d;
            var rendered = message.RenderMessage();
            var line = $"{GetPrefix()}{seconds:F3}s [{name}] {sawmillName}: {rendered}";
            _writer.WriteLine(line);

            if (FailureLevel == null || level < FailureLevel)
                return;

            _writer.Flush();
            Assert.Fail($"{line} Exception: {message.Exception}");
        }

        private string GetPrefix()
        {
            return _prefix != null ? $"{_prefix}: " : "";
        }
    }
}
