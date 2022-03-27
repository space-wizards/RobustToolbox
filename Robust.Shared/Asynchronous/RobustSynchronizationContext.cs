using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Robust.Shared.Exceptions;

namespace Robust.Shared.Asynchronous
{
    internal sealed class RobustSynchronizationContext : SynchronizationContext
    {
        // Used only on release.
        // ReSharper disable once NotAccessedField.Local
        private readonly IRuntimeLog _runtimeLog;

        public RobustSynchronizationContext(IRuntimeLog runtimeLog)
        {
            _runtimeLog = runtimeLog;

            var channel = Channel.CreateUnbounded<Mail>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            _channelReader = channel.Reader;
            _channelWriter = channel.Writer;
        }

        private readonly ChannelReader<Mail> _channelReader;
        private readonly ChannelWriter<Mail> _channelWriter;

        public override void Send(SendOrPostCallback d, object? state)
        {
            if (Current != this)
            {
                // Being invoked from another thread?
                // If this not implemented exception starts being a problem I'll fix it but right now I'd rather err on the side of caution,
                // so that if cross thread usage is required I have a test case, instead of a data race.
                throw new NotImplementedException();
            }

            d(state);
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            _channelWriter.TryWrite(new Mail(d, state));
        }

        public void ProcessPendingTasks()
        {
            while (_channelReader.TryRead(out var task))
            {
#if EXCEPTION_TOLERANCE
                try
#endif
                {
                    task.Callback(task.State);
                }
#if EXCEPTION_TOLERANCE
                catch (Exception e)
                {
                    _runtimeLog.LogException(e, "Async Queued Callback");
                }
#endif
            }
        }

        public ValueTask<bool> WaitOnPendingTasks()
        {
            return _channelReader.WaitToReadAsync();
        }

        private record struct Mail(SendOrPostCallback Callback, object? State);
    }
}
