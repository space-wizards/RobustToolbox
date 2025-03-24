using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Robust.Xaml;

namespace Robust.Build.Tasks
{
    /// <summary>
    /// Based on https://github.com/AvaloniaUI/Avalonia/blob/c85fa2b9977d251a31886c2534613b4730fbaeaf/src/Avalonia.Build.Tasks/CompileAvaloniaXamlTask.cs
    /// </summary>
    public class CompileRobustXamlTask : ITask
    {
        public bool Execute()
        {
            //Debugger.Launch();
            OutputPath = OutputPath ?? AssemblyFile;
            var outputPdb = GetPdbPath(OutputPath);
            var input = AssemblyFile;
            var inputPdb = GetPdbPath(input);
            // Make a copy and delete the original file to prevent MSBuild from thinking that everything is OK
            if (OriginalCopyPath != null)
            {
                File.Copy(AssemblyFile, OriginalCopyPath, true);
                input = OriginalCopyPath;
                File.Delete(AssemblyFile);

                if (File.Exists(inputPdb))
                {
                    var copyPdb = GetPdbPath(OriginalCopyPath);
                    File.Copy(inputPdb, copyPdb, true);
                    File.Delete(inputPdb);
                    inputPdb = copyPdb;
                }
            }

            var msg = $"CompileRobustXamlTask -> AssemblyFile:{AssemblyFile}, ProjectDirectory:{ProjectDirectory}, OutputPath:{OutputPath}";
            BuildEngine.LogMessage(msg, MessageImportance.High);

            var res = XamlAotCompiler.Compile(
                BuildEngine, input,
                File.ReadAllLines(ReferencesFilePath).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray(),
                 OutputPath,
                (SignAssembly && !DelaySign) ? AssemblyOriginatorKeyFile : null
            );
            if (!res.success)
                return false;
            if (!res.writtentofile)
            {
                File.Copy(input, OutputPath, true);
                if(File.Exists(inputPdb))
                    File.Copy(inputPdb, outputPdb, true);
            }

            if (!string.IsNullOrEmpty(UpdateBuildIndicator))
            {
                if (!File.Exists(UpdateBuildIndicator))
                {
                    File.Create(UpdateBuildIndicator).Dispose();
                }
                else
                {
                    File.SetLastWriteTime(UpdateBuildIndicator, DateTime.Now);
                }
            }

            return true;
        }

        // PYREX NOTE: This project was comically null-unsafe before I touched it. I'm just marking what it did accurately
        [Required]
        public string ReferencesFilePath { get; set; } = null!;

        [Required]

        public string ProjectDirectory { get; set; } = null!;

        [Required]
        public string AssemblyFile { get; set; } = null!;

        [Required]
        public string? OriginalCopyPath { get; set; } = null;

        public string? OutputPath { get; set; }
        public string UpdateBuildIndicator { get; set; } = null!;

        public string AssemblyOriginatorKeyFile { get; set; } = null!;
        public bool SignAssembly { get; set; }
        public bool DelaySign { get; set; }

        // shamelessly copied from avalonia
        string GetPdbPath(string p)
        {
            var d = Path.GetDirectoryName(p);
            var f = Path.GetFileNameWithoutExtension(p);
            var rv = f + ".pdb";
            if (d != null)
                rv = Path.Combine(d, rv);
            return rv;
        }

        public IBuildEngine BuildEngine { get; set; } = null!;
        public ITaskHost HostObject { get; set; } = null!;
    }
}
