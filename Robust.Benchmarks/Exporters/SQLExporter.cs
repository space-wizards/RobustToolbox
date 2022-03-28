using System;
using System.Collections.Generic;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace Robust.Benchmarks.Exporters;

public sealed class SQLExporter : IExporter
{
    public static readonly IExporter Default = new SQLExporter();

    private SQLExporter(){}

    public void ExportToLog(Summary summary, ILogger logger)
    {
        Export(summary, logger);
    }

    public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
    {
        Export(summary, consoleLogger);
        return Array.Empty<string>();
    }

    private void Export(Summary summary, ILogger logger)
    {

    }

    public string Name => "sql";
}
