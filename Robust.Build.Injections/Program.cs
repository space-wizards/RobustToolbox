using System;
using System.Collections;
using Microsoft.Build.Framework;

namespace Robust.Build.Injections
{
    class Program
    {
        static int Main(string[] args)
        {
            return new DirtyCallInjectionTask
            {
                AssemblyFile = args[0],
                IntermediatePath = args[1],
                AssemblyReferencesPath = args[2],
                BuildEngine = new ConsoleBuildEngine()
            }.Execute()
                ? 0
                : 2;
        }
        class ConsoleBuildEngine : IBuildEngine
        {
            public void LogErrorEvent(BuildErrorEventArgs e)
            {
                Console.WriteLine($"ERROR: {e.Code} {e.Message} in {e.File} {e.LineNumber}:{e.ColumnNumber}-{e.EndLineNumber}:{e.EndColumnNumber}");
            }

            public void LogWarningEvent(BuildWarningEventArgs e)
            {
                Console.WriteLine($"WARNING: {e.Code} {e.Message} in {e.File} {e.LineNumber}:{e.ColumnNumber}-{e.EndLineNumber}:{e.EndColumnNumber}");
            }

            public void LogMessageEvent(BuildMessageEventArgs e)
            {
                Console.WriteLine($"MESSAGE: {e.Code} {e.Message} in {e.File} {e.LineNumber}:{e.ColumnNumber}-{e.EndLineNumber}:{e.EndColumnNumber}");
            }

            public void LogCustomEvent(CustomBuildEventArgs e)
            {
                Console.WriteLine($"CUSTOM: {e.Message}");
            }

            public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties,
                IDictionary targetOutputs) => throw new NotSupportedException();

            public bool ContinueOnError { get; }
            public int LineNumberOfTaskNode { get; }
            public int ColumnNumberOfTaskNode { get; }
            public string ProjectFileOfTaskNode { get; }
        }
    }
}
