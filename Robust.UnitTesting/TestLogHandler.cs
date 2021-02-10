using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using Serilog.Events;

namespace Robust.UnitTesting
{

    public sealed class TestLogHandler : ILogHandler, IDisposable
    {

        private readonly string _prefix;

        private readonly TextWriter _writer;

        private readonly Stopwatch _sw = Stopwatch.StartNew();

        public TestLogHandler(string prefix)
        {
            _prefix = prefix;
            _writer = TestContext.Out;
            _writer.WriteLine($"{_prefix}: Started {DateTime.Now:o}");
        }

        public void Dispose()
        {
            _writer.Dispose();
        }

        public void Log(string sawmillName, LogEvent message)
        {
            var name = LogMessage.LogLevelToName(message.Level.ToRobust());
            var seconds = _sw.ElapsedMilliseconds/1000d;
            var rendered = message.RenderMessage();
            _writer.WriteLine($"{_prefix}: {seconds:F3}s [{name}] {sawmillName}: {rendered}");
        }

    }

}
