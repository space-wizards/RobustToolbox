using System;
using System.Threading;

namespace Robust.Server.ServerStatus
{

    internal sealed partial class StatusHost
    {

        private SynchronizationContext _syncCtx;

        public void DeferSync(Action a)
        {
            if (ExecuteInlineIfOnSyncCtx(a))
            {
                return;
            }

            _syncCtx.Post(x => ((Action) x)(), a);
        }

        public void WaitSync(Action a, CancellationToken ct = default)
        {
            if (ExecuteInlineIfOnSyncCtx(a))
            {
                return;
            }

            // throws not implemented
            //_syncCtx.Send(x => ((Action) x)(), a);

            using var e = new ManualResetEventSlim(false, 0);
            _syncCtx.Post(_ =>
            {
                a();
                // ReSharper disable once AccessToDisposedClosure
                e.Set();
            }, null);
            e.Wait(ct);
        }

        private bool ExecuteInlineIfOnSyncCtx(Action a)
        {
            if (_syncCtx == SynchronizationContext.Current)
            {
                a();
                return true;
            }

            return false;
        }

    }

}
