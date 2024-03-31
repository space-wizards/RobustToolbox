using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using SpaceWizards.HttpListener;
using Prometheus;
using Robust.Shared.Log;

namespace Robust.Server.DataMetrics;

internal sealed partial class MetricsManager
{
    // prometheus-net's MetricServer uses HttpListener by default.
    // Use our ManagedHttpListener instead because it's less problematic.
    // Also allows us to implement gzip support.

    private sealed class ManagedHttpListenerMetricsServer : MetricHandler
    {
        private readonly ISawmill _sawmill;
        private readonly Func<CancellationToken, Task>? _beforeCollect;
        private readonly HttpListener _listener;
        private readonly CollectorRegistry _registry;

        public ManagedHttpListenerMetricsServer(
            ISawmill sawmill,
            string host,
            int port,
            string url = "metrics/",
            CollectorRegistry? registry = null,
            Func<CancellationToken, Task>? beforeCollect = null)
        {
            _sawmill = sawmill;
            _beforeCollect = beforeCollect;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://{host}:{port}/{url}");
            _registry = registry ?? Metrics.DefaultRegistry;
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
                        MetricsEvents.Log.RequestStart();

                        var resp = ctx.Response;
                        var req = ctx.Request;
                        try
                        {
                            MetricsEvents.Log.ScrapeStart();

                            // prometheus-net does have a "before collect" callback of its own.
                            // But it doesn't get ran before stuff like their System.Diagnostics.Metrics integration,
                            // So I'm just gonna make my own here.
                            if (_beforeCollect != null)
                                await _beforeCollect(cancel);

                            var stream = resp.OutputStream;
                            // prometheus-net is a terrible library and have to do all this insanity,
                            // just to handle the ScrapeFailedException correctly.
                            // Ridiculous.
                            await _registry.CollectAndExportAsTextAsync(new WriteWrapStream(() =>
                            {
                                resp.ContentType = "text/plain; version=0.0.4; charset=utf-8";
                                resp.StatusCode = 200;

                                var acceptEncoding = req.Headers["Accept-Encoding"];
                                var gzip = acceptEncoding != null && acceptEncoding.Contains("gzip");

                                if (gzip)
                                {
                                    stream = new GZipStream(stream, CompressionLevel.Fastest);
                                    resp.Headers["Content-Encoding"] = "gzip";
                                }

                                return stream;
                            }), cancel);

                            await stream.DisposeAsync();

                            MetricsEvents.Log.ScrapeStop();
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

                            MetricsEvents.Log.RequestStop();
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
