using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedHttpListener;
using Prometheus;
using Robust.Shared.Log;

namespace Robust.Server.DataMetrics;

internal sealed partial class MetricsManager
{
    // prometheus-net's MetricServer uses HttpListener by default.
    // Use our ManagedHttpListener instead because it's less problematic.

    private sealed class ManagedHttpListenerMetricsServer : MetricHandler
    {
        private readonly ISawmill _sawmill;
        private readonly HttpListener _listener;

        public ManagedHttpListenerMetricsServer(ISawmill sawmill, string host, int port, string url = "metrics/",
            CollectorRegistry? registry = null) : base(registry)
        {
            _sawmill = sawmill;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://{host}:{port}/{url}");
        }

        protected override Task StartServer(CancellationToken cancel)
        {
            _listener.Start();

            return Task.Run(() => ListenerThread(cancel), CancellationToken.None);
        }

        private async Task ListenerThread(CancellationToken cancel)
        {
            try
            {
                while (!cancel.IsCancellationRequested)
                {
                    var getContextTask = _listener.GetContextAsync();
                    var ctx = await getContextTask.WaitAsync(cancel);

                    // Task.Run this so it gets run on another thread pool thread.
                    _ = Task.Run(async () =>
                    {
                        var resp = ctx.Response;
                        try
                        {
                            // prometheus-net is a terrible library and have to do all this insanity,
                            // just to handle the ScrapeFailedException correctly.
                            // Ridiculous.
                            await _registry.CollectAndExportAsTextAsync(new WriteWrapStream(() =>
                            {
                                resp.ContentType = "text/plain; version=0.0.4; charset=utf-8";
                                resp.StatusCode = 200;

                                return resp.OutputStream;
                            }), cancel);
                        }
                        catch (ScrapeFailedException e)
                        {
                            resp.StatusCode = 503;
                            if (!string.IsNullOrWhiteSpace(e.Message))
                            {
                                await using var sw = new StreamWriter(resp.OutputStream);
                                await sw.WriteAsync(e.Message);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Nada.
                        }
                        catch (Exception e)
                        {
                            _sawmill.Log(LogLevel.Error, e, "Exception in metrics listener");

                            resp.StatusCode = 500;
                        }
                        finally
                        {
                            resp.Close();
                        }
                    }, CancellationToken.None);
                }
            }
            finally
            {
                _listener.Stop();
                _listener.Close();
            }
        }

        private sealed class WriteWrapStream : Stream
        {
            private Stream? _stream;
            private readonly Func<Stream> _streamFunc;

            public WriteWrapStream(Func<Stream> streamFunc)
            {
                _streamFunc = streamFunc;
            }

            public override void Flush()
            {
                GetStream().Flush();
            }

            public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count)
            {
                GetStream().Write(buffer, offset, count);
            }

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                GetStream().Write(buffer);
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer,
                CancellationToken cancellationToken = new CancellationToken())
            {
                return GetStream().WriteAsync(buffer, cancellationToken);
            }

            public override void WriteByte(byte value)
            {
                GetStream().WriteByte(value);
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return GetStream().WriteAsync(buffer, offset, count, cancellationToken);
            }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            private Stream GetStream() => _stream ??= _streamFunc();
        }
    }
}
