using System;
using System.Collections.Generic;
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

    public async Task<(MapFileHandle, OpenFileResult)?> OpenFile(bool readOnly = false)
    {
        var result = await _fileDialog.OpenFile2(MapFileFilters, readOnly ? FileAccess.Read : FileAccess.ReadWrite);
        if (result == null)
            return null;

        var handle = MapFileHandle.CreateUnique();

        _handles[handle] = result.File;
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

    void IDisposable.Dispose()
    {
        foreach (var stream in _handles.Values)
        {
            stream.Dispose();
        }
    }
}
