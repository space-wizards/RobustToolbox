using System;
using System.Collections.Generic;
using System.IO;
using Robust.Shared.GameStates;
using Robust.Shared.Log;
using Robust.Shared.Replays;
using Robust.Shared.Serialization;
using Robust.Shared.Upload;
using Robust.Shared.Utility;

namespace Robust.Client.Replays.Loading;

/// <summary>
/// A windowed <see cref="IReplayDataProvider"/>: instead of keeping every <see cref="GameState"/> and
/// <see cref="ReplayMessage"/> resident, it keeps only a small number of recently-used data blocks
/// loaded and lazily (re)reads blocks from the replay file on demand.
/// </summary>
/// <remarks>
/// Each block corresponds to one <c>data_N</c> file in the replay (≈1 MB uncompressed). When playing
/// back linearly a block boundary is crossed only every few seconds, so the synchronous decompress +
/// deserialize cost is negligible and no background prefetch thread is needed. Jumping around (scrubbing)
/// loads whichever blocks lie between the nearest checkpoint and the target tick; older blocks are
/// evicted once the window limit is exceeded.
/// </remarks>
public sealed class BufferedReplayDataProvider : IReplayDataProvider
{
    /// <summary>
    /// Describes where a contiguous run of ticks lives: which file and the index range it covers.
    /// </summary>
    public readonly struct BlockMeta
    {
        public readonly ResPath FileName;
        public readonly int Start;
        public readonly int Count;

        public BlockMeta(ResPath fileName, int start, int count)
        {
            FileName = fileName;
            Start = start;
            Count = count;
        }
    }

    private sealed class LoadedBlock
    {
        public readonly GameState[] States;
        public readonly ReplayMessage[] Messages;

        public LoadedBlock(GameState[] states, ReplayMessage[] messages)
        {
            States = states;
            Messages = messages;
        }
    }

    private readonly IReplayFileReader _fileReader;
    private readonly IRobustSerializer _serializer;
    private readonly ISawmill _sawmill;
    private readonly BlockMeta[] _blocks;
    private readonly int[] _blockStarts; // parallel to _blocks, for binary search
    private readonly int _maxLoadedBlocks;

    private readonly Dictionary<int, LoadedBlock> _loaded = new();
    private readonly Dictionary<int, LinkedListNode<int>> _lruNodes = new();
    private readonly LinkedList<int> _lru = new(); // most-recently-used at the front

    private bool _disposed;

    public int Count { get; }

    public BufferedReplayDataProvider(
        IReplayFileReader fileReader,
        IRobustSerializer serializer,
        BlockMeta[] blocks,
        int count,
        int maxLoadedBlocks,
        ISawmill sawmill)
    {
        _fileReader = fileReader;
        _serializer = serializer;
        _blocks = blocks;
        _sawmill = sawmill;
        Count = count;
        _maxLoadedBlocks = Math.Max(2, maxLoadedBlocks);

        _blockStarts = new int[blocks.Length];
        for (var i = 0; i < blocks.Length; i++)
            _blockStarts[i] = blocks[i].Start;
    }

    public GameState GetState(int index)
    {
        var blockIdx = ResolveBlockIndex(index);
        var block = GetOrLoad(blockIdx);
        return block.States[index - _blocks[blockIdx].Start];
    }

    public ReplayMessage GetMessages(int index)
    {
        var blockIdx = ResolveBlockIndex(index);
        var block = GetOrLoad(blockIdx);
        return block.Messages[index - _blocks[blockIdx].Start];
    }

    private int ResolveBlockIndex(int index)
    {
        if (index < 0 || index >= Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, $"Replay tick index out of range [0, {Count}).");

        // Find the block whose [Start, Start+Count) range contains index.
        var i = Array.BinarySearch(_blockStarts, index);
        if (i < 0)
            i = ~i - 1; // index falls inside the block that starts before it
        return i;
    }

    private LoadedBlock GetOrLoad(int blockIdx)
    {
        if (_loaded.TryGetValue(blockIdx, out var block))
        {
            Touch(blockIdx);
            return block;
        }

        block = LoadBlock(_blocks[blockIdx]);
        _loaded[blockIdx] = block;
        _lruNodes[blockIdx] = _lru.AddFirst(blockIdx);
        Evict();
        return block;
    }

    private void Touch(int blockIdx)
    {
        var node = _lruNodes[blockIdx];
        if (node.Previous == null)
            return; // already most-recent
        _lru.Remove(node);
        _lru.AddFirst(node);
    }

    private void Evict()
    {
        while (_loaded.Count > _maxLoadedBlocks)
        {
            var lru = _lru.Last!;
            _lru.RemoveLast();
            _loaded.Remove(lru.Value);
            _lruNodes.Remove(lru.Value);
        }
    }

    private LoadedBlock LoadBlock(in BlockMeta meta)
    {
        using var fileStream = _fileReader.Open(meta.FileName);
        using var decompressStream = new ZStdDecompressStream(fileStream, false);

        var intBuf = new byte[4];
        fileStream.ReadExactly(intBuf);
        var uncompressedSize = BitConverter.ToInt32(intBuf);

        var ms = new MemoryStream(uncompressedSize);
        decompressStream.CopyTo(ms);
        ms.Position = 0;

        var states = new GameState[meta.Count];
        var messages = new ReplayMessage[meta.Count];

        var i = 0;
        while (ms.Position < ms.Length)
        {
            _serializer.DeserializeDirect(ms, out GameState state);
            _serializer.DeserializeDirect(ms, out ReplayMessage msg);
            FilterUploadMessages(msg);
            states[i] = state;
            messages[i] = msg;
            i++;
        }

        DebugTools.AssertEqual(i, meta.Count);
        return new LoadedBlock(states, messages);
    }

    /// <summary>
    /// Prototype and resource uploads are consumed once during checkpoint generation (they are removed
    /// from the message list there via RemoveSwap). When a block is re-read for playback we must drop
    /// them again, otherwise they would be re-dispatched and the playback code asserts they are absent.
    /// All other message types (cvar changes, PVS leaves, entity events) are required for playback.
    /// </summary>
    private static void FilterUploadMessages(ReplayMessage msg)
    {
        msg.Messages.RemoveAll(static m =>
            m is ReplayPrototypeUploadMsg
            || m is SharedNetworkResourceManager.ReplayResourceUploadMsg);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _loaded.Clear();
        _lruNodes.Clear();
        _lru.Clear();
        _fileReader.Dispose();
    }
}
