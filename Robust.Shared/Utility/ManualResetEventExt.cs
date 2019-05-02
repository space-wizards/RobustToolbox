using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Robust.Shared.Utility
{
    [PublicAPI]
    public static class ManualResetEventExt
    {
        // https://thomaslevesque.com/2015/06/04/async-and-cancellation-support-for-wait-handles/
        public static async Task<bool> WaitOneAsync(this WaitHandle handle, int millisecondsTimeout,
            CancellationToken cancellationToken = default)
        {
            RegisteredWaitHandle registeredHandle = null;
            CancellationTokenRegistration tokenRegistration = default;
            try
            {
                var tcs = new TaskCompletionSource<bool>();
                registeredHandle = ThreadPool.RegisterWaitForSingleObject(
                    handle,
                    (state, timedOut) => ((TaskCompletionSource<bool>) state).TrySetResult(!timedOut),
                    tcs,
                    millisecondsTimeout,
                    true);
                if (cancellationToken != default)
                {
                    tokenRegistration = cancellationToken.Register(
                        state => ((TaskCompletionSource<bool>) state).TrySetCanceled(),
                        tcs);
                }

                return await tcs.Task;
            }
            finally
            {
                registeredHandle?.Unregister(null);
                if (tokenRegistration != default)
                {
                    tokenRegistration.Dispose();
                }
            }
        }

        public static Task<bool> WaitOneAsync(this WaitHandle handle, TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            return handle.WaitOneAsync((int) timeout.TotalMilliseconds, cancellationToken);
        }

        public static Task<bool> WaitOneAsync(this WaitHandle handle, CancellationToken cancellationToken = default)
        {
            return handle.WaitOneAsync(Timeout.Infinite, cancellationToken);
        }
    }
}
