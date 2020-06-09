using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Robust.Shared.Utility
{
    public static class ProcessExt
    {
        public static async Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<bool>();

            void ProcessExited(object? sender, EventArgs e)
            {
                tcs.TrySetResult(true);
            }

            process.EnableRaisingEvents = true;
            process.Exited += ProcessExited;

            try
            {
                if (process.HasExited)
                {
                    return;
                }

                using (cancellationToken.Register(() => tcs.TrySetCanceled()))
                {
                    await tcs.Task;
                }
            }
            finally
            {
                process.Exited -= ProcessExited;
            }
        }

    }
}
