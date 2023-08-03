using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Channels;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Threading.Tasks;
using Robust.Shared.Log;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Replays;

// This partial class has various methods for async file writing (in case the path is on a networked drive or something like that)
internal abstract partial class SharedReplayRecordingManager
{
    // To avoid stuttering the main thread, we do IO (and writing to the zip in general) in the thread pool.

    // While recording a replay, the Task for the write queue is stored in the RecordingState.
    // However when the replay recording gets finished, we immediately clear _recState before the write queue is finished.
    // As such, we need to track the task here.
    // In practice, this list will most likely never contain more than a single element,
    // and even then not for much longer than a couple hundred ms at most.
    private readonly List<Task> _finalizingWriteTasks = new();

    private static void WriteYaml(RecordingState state, ResPath path, YamlDocument data)
    {
        var memStream = new MemoryStream();
        using var writer = new StreamWriter(memStream);
        var yamlStream = new YamlStream { data };
        yamlStream.Save(new YamlMappingFix(new Emitter(writer)), false);
        writer.Flush();
        WriteBytes(state, path, memStream.AsMemory());
    }

    private void WriteSerializer<T>(RecordingState state, ResPath path, T obj)
    {
        var memStream = new MemoryStream();
        _serializer.SerializeDirect(memStream, obj);

        WriteBytes(state, path, memStream.AsMemory());
    }

    private static void WritePooledBytes(
        RecordingState state,
        ResPath path,
        byte[] bytes,
        int length,
        CompressionLevel compression)
    {
        DebugTools.Assert(path.IsRelative, "Zip path should be relative");

        WriteQueueTask(state, () =>
        {
            try
            {
                var entry = state.Zip.CreateEntry(path.ToString(), compression);
                using var stream = entry.Open();
                stream.Write(bytes, 0, length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        });
    }

    private void WriteToml(RecordingState state, IEnumerable<string> enumerable, ResPath path)
    {
        var memStream = new MemoryStream();
        NetConf.SaveToTomlStream(memStream, enumerable);

        WriteBytes(state, path, memStream.AsMemory());
    }

    private static void WriteBytes(
        RecordingState recState,
        ResPath path,
        ReadOnlyMemory<byte> bytes,
        CompressionLevel compression = CompressionLevel.Optimal)
    {
        DebugTools.Assert(path.IsRelative, "Zip path should be relative");

        WriteQueueTask(recState, () =>
        {
            var entry = recState.Zip.CreateEntry(path.ToString(), compression);
            using var stream = entry.Open();
            stream.Write(bytes.Span);
        });
    }

    private static void WriteQueueTask(RecordingState recState, Action a)
    {
        var task = recState.WriteCommandChannel.WriteAsync(a);

        // If we have to wait here, it's because the channel is full.
        // Synchronous waiting is safe here: the writing code doesn't rely on the synchronization context.
        if (!task.IsCompletedSuccessfully)
            task.AsTask().Wait();
    }

    protected void UpdateWriteTasks()
    {
        if (_recState != null)
        {
            // We are actively recording a replay. Check the status of the write task to make sure nothing went wrong.
            if (_recState.WriteTask.IsFaulted)
            {
                _sawmill.Log(
                    LogLevel.Error,
                    _recState.WriteTask.Exception,
                    "Write task failed while recording due to exception, aborting recording!");

                Reset();
            }
            else if (_recState.WriteTask.IsCompleted)
            {
                // This shouldn't be possible since the write task only exits if we close the channel,
                // which we only do while clearing _recState.
                _sawmill.Error("Write task completed, but did not report an error?");
            }
        }

        for (var i = _finalizingWriteTasks.Count - 1; i >= 0; i--)
        {
            var task = _finalizingWriteTasks[i];
            if (task.IsCompletedSuccessfully)
            {
                _sawmill.Debug("Write task finalized cleanly");
            }
            else if (task.IsFaulted)
            {
                _sawmill.Log(
                    LogLevel.Error,
                    task.Exception,
                    "Write task hit exception while finalizing, replay may have been corrupted!");
            }

            if (task.IsCompleted)
                _finalizingWriteTasks.RemoveSwap(i);
        }
    }

    public Task WaitWriteTasks()
    {
        if (IsRecording)
            throw new InvalidOperationException("Cannot wait for writes to finish while still recording replay");

        // First, check for any tasks that have encountered errors.
        UpdateWriteTasks();

        return Task.WhenAll(_finalizingWriteTasks);
    }

    private static async Task WriteQueueLoop(ChannelReader<Action> reader, ZipArchive archive)
    {
        try
        {
            while (true)
            {
                var result = await reader.WaitToReadAsync();
                if (!result)
                    break;

                var action = await reader.ReadAsync();
                action();
            }
        }
        finally
        {
            archive.Dispose();
        }
    }
}
