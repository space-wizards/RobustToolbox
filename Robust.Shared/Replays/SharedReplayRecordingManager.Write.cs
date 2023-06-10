using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Replays;

// This partial class has various methods for async file writing (in case the path is on a networked drive or something like that)
internal abstract partial class SharedReplayRecordingManager
{
    private List<Task> _writeTasks = new();

    private void WriteYaml(YamlDocument data, IWritableDirProvider dir, ResPath path)
    {
        var memStream = new MemoryStream();
        using var writer = new StreamWriter(memStream);
        var yamlStream = new YamlStream { data };
        yamlStream.Save(new YamlMappingFix(new Emitter(writer)), false);
        writer.Flush();
        var task = Task.Run(() => dir.WriteAllBytesAsync(path, memStream.ToArray()));
        _writeTasks.Add(task);
    }

    private void WriteSerializer<T>(T obj, IWritableDirProvider dir, ResPath path)
    {
        var memStream = new MemoryStream();
        _serializer.SerializeDirect(memStream, obj);

        var task = Task.Run(() => dir.WriteAllBytesAsync(path, memStream.ToArray()));
        _writeTasks.Add(task);
    }

    private void WriteBytes(byte[] bytes, IWritableDirProvider dir, ResPath path)
    {
        var task = Task.Run(() => dir.WriteAllBytesAsync(path, bytes));
        _writeTasks.Add(task);
    }

    private void WritePooledBytes(byte[] bytes, int length, IWritableDirProvider dir, ResPath path)
    {
        var task = Task.Run(() => Write(bytes, length, dir, path));
        _writeTasks.Add(task);

        static async Task Write(byte[] bytes, int length, IWritableDirProvider dir, ResPath path)
        {
            try
            {
                var slice = new ReadOnlyMemory<byte>(bytes, 0, length);
                await dir.WriteBytesAsync(path, slice);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }
    }

    private void WriteToml(IEnumerable<string> enumerable, IWritableDirProvider dir, ResPath path)
    {
        var memStream = new MemoryStream();
        NetConf.SaveToTomlStream(memStream, enumerable);

        var task = Task.Run(() => dir.WriteAllBytesAsync(path, memStream.ToArray()));
        _writeTasks.Add(task);
    }

    protected bool UpdateWriteTasks()
    {
        bool isWriting = false;
        for (var i = _writeTasks.Count - 1; i >= 0; i--)
        {
            var task = _writeTasks[i];
            switch(task.Status)
            {
                case TaskStatus.Canceled:
                case TaskStatus.RanToCompletion:
                    _writeTasks.RemoveSwap(i);
                    break;

                case TaskStatus.Faulted:
                    var ex = task.Exception;
                    _sawmill.Error($"Replay write task encountered a fault. Rethrowing exception");
                    Reset();
                    throw ex!;

                case TaskStatus.Created:
                    Reset();
                    throw new Exception("A replay write task was never started?");

                default:
                    isWriting = true;
                    break;
            }
        }

        return isWriting;
    }

    public bool IsWriting() => UpdateWriteTasks();

    public Task WaitWriteTasks()
    {
        if (IsRecording)
            throw new InvalidOperationException("Cannot wait for writes to finish while still recording replay");

        // First, check for any tasks that have encountered errors.
        UpdateWriteTasks();

        return Task.WhenAll(_writeTasks);
    }
}
