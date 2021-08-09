using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using Robust.Shared.Log;
using Serilog.Events;

namespace Robust.UnitTesting
{
    public sealed class TestLogHandler : ILogHandler, IDisposable
    {
        private readonly string? _prefix;

        private readonly TextWriter _writer;

        private readonly Stopwatch _sw = Stopwatch.StartNew();

        private readonly LogLevel? _failureLevel;

        public TestLogHandler(string? prefix = null, LogLevel? failureLevel = null)
        {
            _prefix = prefix;
            _failureLevel = failureLevel;
            _writer = TestContext.Out;
            _writer.WriteLine($"{GetPrefix()}Started {DateTime.Now:o}");
        }

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

            if (_failureLevel == null || level < _failureLevel)
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
