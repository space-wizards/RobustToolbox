using Robust.Shared.Interfaces.Log;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;
using Robust.Shared.Log;
using Robust.Shared.Utility;

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

        public void Log(in LogMessage message)
        {
            var name = message.LogLevelToName();
            var seconds = _sw.ElapsedMilliseconds/1000d;
            _writer.WriteLine($"{_prefix}: {seconds:F3}s [{name}] {message.SawmillName}: {message.Message}");
        }

    }

}
