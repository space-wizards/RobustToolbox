using System;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.Network.Transfer;

internal sealed class TransferTestCommand : IConsoleCommand
{
    internal const string CommandKey = "transfer_test";

    [Dependency] private readonly ITransferManager _transferManager = null!;

    public string Command => CommandKey;
    public string Description => "";
    public string Help => "Usage: transfer_test <buffer count>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player?.Channel is not { } channel)
        {
            shell.WriteError("You do not have a channel");
            return;
        }

        var bufferCount = 1024;
        if (args.Length >= 1)
            bufferCount = Parse.Int32(args[0]);

        await using var stream = _transferManager.StartTransfer(channel,
            new TransferStartInfo
            {
                MessageKey = TransferTestManager.Key,
            });

        var buffer = new byte[16384];
        for (var i = 0; i < bufferCount; i++)
        {
            await stream.WriteAsync(buffer).ConfigureAwait(false);
        }
    }
}

internal abstract class TransferTestManager(ITransferManager manager, ILogManager logManager)
{
    private readonly ISawmill _sawmill = logManager.GetSawmill("net.transfer.test");

    internal const string Key = nameof(TransferTestManager);

    public void Initialize()
    {
        manager.RegisterTransferMessage(Key, RxCallback);
    }

    // ReSharper disable once AsyncVoidMethod
    private async void RxCallback(TransferReceivedEvent receive)
    {
        if (!PermissionCheck(receive.Channel))
        {
            receive.Channel.Disconnect("Not allowed");
            return;
        }

        _sawmill.Info("Receiving debug transfer");

        var buffer = new byte[16384];
        var totalRead = 0L;
        while (true)
        {
            var read = await receive.DataStream.ReadAsync(buffer.AsMemory()).ConfigureAwait(false);
            totalRead += read;
            if (read == 0)
                break;
        }

        _sawmill.Info($"Debug transfer complete for {ByteHelpers.FormatKibibytes(totalRead)} bytes");
    }

    protected abstract bool PermissionCheck(INetChannel channel);
}
