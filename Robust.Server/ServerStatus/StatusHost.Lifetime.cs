using System.Threading;

namespace Robust.Server.ServerStatus
{

    internal sealed partial class StatusHost
    {

        private readonly CancellationTokenSource _startedSource = new CancellationTokenSource();

        private readonly CancellationTokenSource _stoppedSource = new CancellationTokenSource();

        private readonly CancellationTokenSource _stoppingSource = new CancellationTokenSource();

        public void StopApplication() => Dispose();

        /// <summary>
        /// Triggered when the application host has fully started and is about to wait
        /// for a graceful shutdown.
        /// </summary>
        public CancellationToken ApplicationStarted => _startedSource.Token;

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// Request may still be in flight. Shutdown will block until this event completes.
        /// </summary>
        public CancellationToken ApplicationStopping => _stoppingSource.Token;

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// All requests should be complete at this point. Shutdown will block
        /// until this event completes.
        /// </summary>
        public CancellationToken ApplicationStopped => _stoppedSource.Token;

        public void Dispose()
        {
            if (_stoppingSource.IsCancellationRequested)
            {
                return;
            }

            _stoppingSource.Cancel();
            _server.StopAsync(ApplicationStopped);
            _stoppedSource.Cancel();
        }

    }

}
