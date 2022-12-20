using System.Threading;
using System.Threading.Tasks;

namespace Robust.Shared.Utility;

internal static class WaitHandleHelpers
{
    // https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/interop-with-other-asynchronous-patterns-and-types?redirectedfrom=MSDN#from-wait-handles-to-tap
    public static Task WaitOneAsync(WaitHandle handle)
    {
        var tcs = new TaskCompletionSource();
        var rwh = ThreadPool.RegisterWaitForSingleObject(
            handle,
            delegate { tcs.TrySetResult(); },
            null,
            -1,
            true);

        var t = tcs.Task;
        t.ContinueWith(_ => rwh.Unregister(null));
        return t;
    }

}
