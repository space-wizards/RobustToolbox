using System;
using System.Diagnostics;
using System.Text;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared.Utility;

namespace SS14.Client.UserInterface
{
    internal sealed class ClipboardManagerLinux : IClipboardManager
    {
        public bool Available { get; }

        public string NotAvailableReason =>
            // ReSharper disable once StringLiteralTypo
            "Clipboard support on Linux is done with the 'xclip' utility. Please install it.";

        public string GetText()
        {
            if (!Available)
            {
                throw new NotSupportedException();
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "xclip",
                Arguments = "-o -selection clipboard",
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8,
                UseShellExecute = false,
            };

            var process = Process.Start(startInfo);
            DebugTools.AssertNotNull(process);
            process.WaitForExit();
            return process.StandardOutput.ReadToEnd();
        }

        public void SetText(string text)
        {
            if (!Available)
            {
                throw new NotSupportedException();
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "xclip",
                Arguments = "-i -selection clipboard",
                RedirectStandardInput = true,
                UseShellExecute = false,
            };

            var process = Process.Start(startInfo);
            DebugTools.AssertNotNull(process);
            process.StandardInput.Write(text);
            process.StandardInput.Close();
            process.WaitForExit();
        }

        public ClipboardManagerLinux()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "xclip",
                        Arguments = "-version",
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    }
                };
                process.Start();
                process.WaitForExit();
                Available = process.ExitCode == 0;
            }
            catch (Exception)
            {
                Available = false;
            }

            Available = false;
        }
    }
}
