using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Channels;
using Robust.Shared3D;

namespace Robust.Client3D;

internal sealed class NetworkClient3D : IDisposable
{
    private readonly TcpClient _socket;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Channel<InputMessage3D> _outgoing = Channel.CreateBounded<InputMessage3D>(
        new BoundedChannelOptions(256)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = true,
        });
    private readonly ConcurrentQueue<SnapshotMessage3D> _snapshots = new();
    private readonly TaskCompletionSource<HelloMessage3D> _hello = new(
        TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Task _readerTask;
    private readonly Task _writerTask;

    public int PlayerId { get; private set; }
    public bool Connected => _socket.Connected && !_cancellation.IsCancellationRequested;

    private NetworkClient3D(TcpClient socket)
    {
        _socket = socket;
        _reader = new StreamReader(socket.GetStream());
        _writer = new StreamWriter(socket.GetStream()) { AutoFlush = true };
        _readerTask = ReadLoopAsync(_cancellation.Token);
        _writerTask = WriteLoopAsync(_cancellation.Token);
    }

    public static async Task<NetworkClient3D> ConnectAsync(
        string host,
        int port,
        CancellationToken cancellationToken)
    {
        var socket = new TcpClient { NoDelay = true };
        await socket.ConnectAsync(host, port, cancellationToken);
        var client = new NetworkClient3D(socket);

        try
        {
            var hello = await client._hello.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            if (hello.ProtocolVersion != NetworkProtocol3D.Version)
            {
                throw new InvalidOperationException(
                    $"3D protocol mismatch: server={hello.ProtocolVersion}, client={NetworkProtocol3D.Version}");
            }

            client.PlayerId = hello.PlayerId;
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    public bool QueueInput(InputMessage3D input)
    {
        return _outgoing.Writer.TryWrite(input);
    }

    public bool TryReadLatestSnapshot(out SnapshotMessage3D snapshot)
    {
        snapshot = null!;
        while (_snapshots.TryDequeue(out var next))
            snapshot = next;

        return snapshot is not null;
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await _reader.ReadLineAsync(cancellationToken);
                if (line is null)
                    break;

                switch (NetworkProtocol3D.ReadKind(line))
                {
                    case "hello":
                        var hello = NetworkProtocol3D.Deserialize<HelloMessage3D>(line);
                        if (hello is not null)
                            _hello.TrySetResult(hello);
                        break;

                    case "snapshot":
                        var snapshot = NetworkProtocol3D.Deserialize<SnapshotMessage3D>(line);
                        if (snapshot is not null)
                            _snapshots.Enqueue(snapshot);
                        break;
                }
            }
        }
        catch (Exception exception) when (exception is IOException or SocketException or OperationCanceledException)
        {
            _hello.TrySetException(exception);
        }
        finally
        {
            _cancellation.Cancel();
        }
    }

    private async Task WriteLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var input in _outgoing.Reader.ReadAllAsync(cancellationToken))
            {
                await _writer.WriteLineAsync(
                    NetworkProtocol3D.Serialize(input).AsMemory(),
                    cancellationToken);
            }
        }
        catch (Exception exception) when (exception is IOException or SocketException or OperationCanceledException)
        {
        }
        finally
        {
            _cancellation.Cancel();
        }
    }

    public void Dispose()
    {
        if (_cancellation.IsCancellationRequested)
            return;

        _cancellation.Cancel();
        _outgoing.Writer.TryComplete();
        _socket.Dispose();
        _reader.Dispose();
        _writer.Dispose();

        try
        {
            Task.WaitAll(new[] { _readerTask, _writerTask }, TimeSpan.FromSeconds(1));
        }
        catch (AggregateException)
        {
        }

        _cancellation.Dispose();
    }
}
