using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Robust.Client.UserInterface;
using Robust.Shared.Log;
using Robust.Shared.MapEditor;

namespace Robust.Client.MapEditor;

/// <summary>
/// Keeps track of opened map file handles in the map editor.
/// </summary>
internal sealed class MapFileHandleManager : IDisposable
{
    private static readonly FileDialogFilters MapFileFilters = new(new FileDialogFilters.Group("yml"));

    private readonly IFileDialogManager _fileDialog;
    private readonly ISawmill _sawmill;

    private readonly Dictionary<MapFileHandle, Stream> _handles = [];

    public MapFileHandleManager(IFileDialogManager fileDialog, ILogManager logManager)
    {
        _fileDialog = fileDialog;
        _sawmill = logManager.GetSawmill("map_editor.file_handles");
    }

    public MapFileHandle CreateHandleForExistingStream(Stream stream)
    {
        if (!stream.CanSeek)
            throw new ArgumentException("Stream must be seekable", nameof(stream));

        var handle = MapFileHandle.CreateUnique();

        _handles[handle] = stream;

        return handle;
    }

    public async Task<(MapFileHandle Handle, OpenFileResult FileResult)?> SaveFile()
    {
        return await HandleFileDialogCore(_fileDialog.SaveFile2(MapFileFilters, share: FileShare.Read));
    }

    public async Task<(MapFileHandle Handle, OpenFileResult FileResult)?> OpenFile(bool readOnly = false)
    {
        return await HandleFileDialogCore(
            _fileDialog.OpenFile2(MapFileFilters, readOnly ? FileAccess.Read : FileAccess.ReadWrite, share: FileShare.Read));
    }

    private async Task<(MapFileHandle Handle, OpenFileResult FileResult)?> HandleFileDialogCore(
        Task<OpenFileResult?> task)
    {
        var result = await task;
        if (result == null)
            return null;

        var handle = CreateHandleForExistingStream(result.File);

        _sawmill.Debug($"Opened map file {result.FileName} handle {handle}");

        return (handle, result);
    }

    public void CloseHandle(MapFileHandle handle)
    {
        if (_handles.Remove(handle, out var value))
        {
            _sawmill.Debug($"Closing map file handle {handle}");
            value.Dispose();
        }
    }

    public bool TryGetStream(MapFileHandle handle, [NotNullWhen(true)] out Stream? stream)
    {
        return _handles.TryGetValue(handle, out stream);
    }

    void IDisposable.Dispose()
    {
        foreach (var stream in _handles.Values)
        {
            stream.Dispose();
        }
    }
}
