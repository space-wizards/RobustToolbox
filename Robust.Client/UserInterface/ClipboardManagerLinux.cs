using System;
using System.Diagnostics;
using Robust.Client.Interfaces.UserInterface;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface
{
    internal sealed class ClipboardManagerLinux : IClipboardManagerInternal
    {
        public bool Available { get; private set; }

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
                StandardOutputEncoding = EncodingHelpers.UTF8,
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

        public async void Initialize()
        {
            try
            {
                var process = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "xclip",
                        Arguments = "-version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    });

                if (process == null)
                {
                    Available = false;
                    return;
                }

                await process.WaitForExitAsync();
                Available = process.ExitCode == 0;
            }
            catch (Exception)
            {
                Available = false;
            }
        }
    }
}
