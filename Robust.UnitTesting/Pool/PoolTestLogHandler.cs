using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Serilog.Events;

namespace Robust.UnitTesting.Pool;

/// <summary>
/// Log handler intended for pooled integration tests.
/// </summary>
/// <remarks>
/// <para>
/// This class logs to one place: an NUnit <see cref="TestContext"/> (so it nicely gets attributed to a test in your IDE)
/// </para>
/// <para>
/// The active test context can be swapped out so pooled instances can correctly have their logs attributed.
/// </para>
/// </remarks>
public sealed class PoolTestLogHandler : ILogHandler
{
    private readonly string? _prefix;

    private RStopwatch _stopwatch;

    public TextWriter? ActiveContext { get; private set; }

    public LogLevel? FailureLevel { get; set; }

    public IReadOnlyList<string> FailingLogs => _failingLogs;

    private readonly List<string> _failingLogs = new();

    public PoolTestLogHandler(string? prefix)
    {
        _prefix = prefix != null ? $"{prefix}: " : "";
    }

    public bool ShuttingDown;

    /// <summary>
    /// <para>
    ///     Event handler that allows you to override a potential failing log.
    ///     Use this if you want to allow certain error logs to be considered passing.
    /// </para>
    /// <para>
    ///     Has the sawmill name and <see cref="LogEvent"/> passed in, and should return a boolean
    ///     <see langword="true"/> when the log message should not be logged as a failure.
    /// </para>
    /// </summary>
    public event Func<string, LogEvent, bool>? JudgeLog;

    public void Log(string sawmillName, LogEvent message)
    {
        var level = message.Level.ToRobust();

        if (ShuttingDown && (FailureLevel == null || level < FailureLevel))
            return;

        if (ActiveContext is not { } testContext)
        {
            // If this gets hit it means something is logging to this instance while it's "between" tests.
            // This is a bug in either the game or the testing system, and must always be investigated.
            throw new InvalidOperationException("Log to pool test log handler without active test context");
        }

        var name = LogMessage.LogLevelToName(level);
        var seconds = _stopwatch.Elapsed.TotalSeconds;
        var rendered = message.RenderMessage();
        var line = $"{_prefix}{seconds:F3}s [{name}] {sawmillName}: {rendered}";

        testContext.WriteLine(line);

        if (FailureLevel == null || level < FailureLevel || (JudgeLog?.Invoke(sawmillName, message) ?? false))
            return;

        testContext.Flush();
        _failingLogs.Add($"{line} Exception: {message.Exception}");
    }

    public void ClearContext()
    {
        ActiveContext = null;
        _failingLogs.Clear();
    }

    public void ActivateContext(TextWriter context)
    {
        _stopwatch.Restart();
        ActiveContext = context;
    }
}
