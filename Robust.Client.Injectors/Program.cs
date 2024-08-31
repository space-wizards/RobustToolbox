using System;
using System.Collections;
using System.IO;
using Microsoft.Build.Framework;

namespace Robust.Build.Tasks
{
    /// <summary>
    /// Based on https://github.com/AvaloniaUI/Avalonia/blob/c85fa2b9977d251a31886c2534613b4730fbaeaf/src/Avalonia.Build.Tasks/Program.cs
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("expected: input references output");
                return 1;
            }

            return new CompileRobustXamlTask
            {
                AssemblyFile = args[0],
                ReferencesFilePath = args[1],
                OutputPath = args[2],
                BuildEngine = new ConsoleBuildEngine(),
                ProjectDirectory = Directory.GetCurrentDirectory()
            }.Execute() ? 0 : 2;
        }
    }

    //formatted according to https://github.com/dotnet/msbuild/blob/main/src/Shared/CanonicalError.cs#L57
    class ConsoleBuildEngine : IBuildEngine
    {
        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            Console.WriteLine($"{e.File} ({e.LineNumber},{e.ColumnNumber},{e.EndLineNumber},{e.EndColumnNumber}): XAMLIL ERROR {e.Code}: {e.Message}");
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
            Console.WriteLine($"{e.File} ({e.LineNumber},{e.ColumnNumber},{e.EndLineNumber},{e.EndColumnNumber}): XAMLIL WARNING {e.Code}: {e.Message}");
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            Console.WriteLine($"{e.File} ({e.LineNumber},{e.ColumnNumber},{e.EndLineNumber},{e.EndColumnNumber}): XAMLIL MESSAGE {e.Code}: {e.Message}");
        }

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties,
            IDictionary targetOutputs) => throw new NotSupportedException();

        // PYREX NOTE: This project was extremely null-unsafe before I touched it. I'm just marking what it did already
        // Here's the broken interface of IBuildEngine that we started with
        public bool ContinueOnError => default;
        public int LineNumberOfTaskNode => default;
        public int ColumnNumberOfTaskNode => default;
        public string ProjectFileOfTaskNode => null!;
    }
}
