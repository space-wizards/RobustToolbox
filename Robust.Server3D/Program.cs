using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Robust.Shared3D;

namespace Robust.Server3D;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var port = ReadInteger(args, "--port=", NetworkProtocol3D.DefaultPort);
            var tickLimit = ReadInteger(args, "--ticks=", 0);
            using var cancellation = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cancellation.Cancel();
            };

            var server = new AuthoritativeServer3D(port);
            await server.RunAsync(tickLimit > 0 ? tickLimit : null, cancellation.Token);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static int ReadInteger(string[] args, string prefix, int fallback)
    {
        foreach (var argument in args)
        {
            if (argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(argument[prefix.Length..], out var value))
            {
                return value;
            }
        }

        return fallback;
    }
}

internal sealed class AuthoritativeServer3D
{
    private const int SnapshotIntervalTicks = 6;

    private readonly TcpListener _listener;
    private readonly ConcurrentQueue<ServerCommand3D> _commands = new();
    private readonly Dictionary<int, ClientConnection3D> _clients = new();
    private readonly AuthoritativeWorld3D _world = new();
    private int _nextPlayerId;

    public AuthoritativeServer3D(int port)
    {
        _listener = new TcpListener(IPAddress.Loopback, port);
    }

    public async Task RunAsync(int? tickLimit, CancellationToken cancellationToken)
    {
        _listener.Start();
        var endpoint = (IPEndPoint) _listener.LocalEndpoint;
        Console.WriteLine($"Server3D listening on {endpoint.Address}:{endpoint.Port}");

        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var acceptTask = AcceptLoopAsync(linkedCancellation.Token);
        var stopwatch = Stopwatch.StartNew();
        var nextTick = stopwatch.Elapsed;

        try
        {
            while (!linkedCancellation.IsCancellationRequested &&
                   (tickLimit is null || _world.Tick < tickLimit.Value))
            {
                DrainCommands();
                _world.Step();

                if (_world.Tick % SnapshotIntervalTicks == 0)
                    await BroadcastSnapshotAsync(linkedCancellation.Token);

                nextTick += TimeSpan.FromSeconds(AuthoritativeWorld3D.FixedDelta);
                var remaining = nextTick - stopwatch.Elapsed;
                if (remaining > TimeSpan.Zero)
                    await Task.Delay(remaining, linkedCancellation.Token);
            }
        }
        catch (OperationCanceledException) when (linkedCancellation.IsCancellationRequested)
        {
        }
        finally
        {
            linkedCancellation.Cancel();
            _listener.Stop();

            foreach (var client in _clients.Values)
                client.Dispose();

            try
            {
                await acceptTask;
            }
            catch (OperationCanceledException)
            {
            }

            Console.WriteLine($"Server3D stopped at tick {_world.Tick}; players={_world.Players.Count}");
        }
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient socket;
            try
            {
                socket = await _listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (SocketException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            socket.NoDelay = true;
            var playerId = Interlocked.Increment(ref _nextPlayerId);
            var connection = new ClientConnection3D(playerId, socket, _commands);
            _commands.Enqueue(new ConnectCommand3D(connection));
            _ = connection.ReadLoopAsync(cancellationToken);
        }
    }

    private void DrainCommands()
    {
        while (_commands.TryDequeue(out var command))
        {
            switch (command)
            {
                case ConnectCommand3D connect:
                    _clients.Add(connect.Connection.PlayerId, connect.Connection);
                    _world.AddPlayer(connect.Connection.PlayerId);
                    _ = connect.Connection.SendAsync(new HelloMessage3D
                    {
                        PlayerId = connect.Connection.PlayerId,
                        FixedDelta = AuthoritativeWorld3D.FixedDelta,
                    }, CancellationToken.None);
                    Console.WriteLine($"Player {connect.Connection.PlayerId} connected");
                    break;

                case InputCommand3D input:
                    _world.ApplyInput(input.PlayerId, input.Input);
                    break;

                case DisconnectCommand3D disconnect:
                    if (_clients.Remove(disconnect.PlayerId, out var connection))
                    {
                        connection.Dispose();
                        _world.RemovePlayer(disconnect.PlayerId);
                        Console.WriteLine($"Player {disconnect.PlayerId} disconnected");
                    }
                    break;
            }
        }
    }

    private async Task BroadcastSnapshotAsync(CancellationToken cancellationToken)
    {
        if (_clients.Count == 0)
            return;

        var snapshot = _world.CreateSnapshot();
        var sends = _clients.Values.Select(client => client.SendAsync(snapshot, cancellationToken));
        await Task.WhenAll(sends);
    }
}

internal abstract record ServerCommand3D;
internal sealed record ConnectCommand3D(ClientConnection3D Connection) : ServerCommand3D;
internal sealed record DisconnectCommand3D(int PlayerId) : ServerCommand3D;
internal sealed record InputCommand3D(int PlayerId, InputMessage3D Input) : ServerCommand3D;

internal sealed class ClientConnection3D : IDisposable
{
    private readonly TcpClient _socket;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly ConcurrentQueue<ServerCommand3D> _commands;
    private int _disposed;

    public int PlayerId { get; }

    public ClientConnection3D(
        int playerId,
        TcpClient socket,
        ConcurrentQueue<ServerCommand3D> commands)
    {
        PlayerId = playerId;
        _socket = socket;
        _commands = commands;
        _reader = new StreamReader(socket.GetStream());
        _writer = new StreamWriter(socket.GetStream()) { AutoFlush = true };
    }

    public async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await _reader.ReadLineAsync(cancellationToken);
                if (line is null)
                    break;

                if (NetworkProtocol3D.ReadKind(line) != "input")
                    continue;

                var input = NetworkProtocol3D.Deserialize<InputMessage3D>(line);
                if (input is not null)
                    _commands.Enqueue(new InputCommand3D(PlayerId, input));
            }
        }
        catch (Exception exception) when (exception is IOException or SocketException or OperationCanceledException)
        {
        }
        finally
        {
            _commands.Enqueue(new DisconnectCommand3D(PlayerId));
        }
    }

    public async Task SendAsync<T>(T message, CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _disposed) != 0)
            return;

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await _writer.WriteLineAsync(NetworkProtocol3D.Serialize(message).AsMemory(), cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or SocketException or ObjectDisposedException)
        {
            _commands.Enqueue(new DisconnectCommand3D(PlayerId));
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _socket.Dispose();
        _reader.Dispose();
        _writer.Dispose();
        _writeLock.Dispose();
    }
}
