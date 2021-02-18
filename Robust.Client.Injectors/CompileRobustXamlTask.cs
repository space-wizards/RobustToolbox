using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

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

            var res = XamlCompiler.Compile(BuildEngine, input,
                File.ReadAllLines(ReferencesFilePath).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray(),
                ProjectDirectory, OutputPath,
                (SignAssembly && !DelaySign) ? AssemblyOriginatorKeyFile : null);
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

        [Required]
        public string ReferencesFilePath { get; set; }

        [Required]
        public string ProjectDirectory { get; set; }

        [Required]
        public string AssemblyFile { get; set; }

        [Required]
        public string OriginalCopyPath { get; set; }

        public string OutputPath { get; set; }
        public string UpdateBuildIndicator { get; set; }

        public string AssemblyOriginatorKeyFile { get; set; }
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

        public IBuildEngine BuildEngine { get; set; }
        public ITaskHost HostObject { get; set; }
    }
}
