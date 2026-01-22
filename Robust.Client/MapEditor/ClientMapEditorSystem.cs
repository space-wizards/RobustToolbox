using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.MapEditor;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using MEM = Robust.Shared.MapEditor.MapEditorMessages;

namespace Robust.Client.MapEditor;

internal sealed class ClientMapEditorSystem : MapEditorSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = null!;
    [Dependency] private readonly IStateManager _stateManager = null!;
    [Dependency] private readonly MapFileHandleManager _mapFileHandles = null!;

    internal event Action<IEnumerable<EntityUid>>? OpenMapsUpdated;
    internal event Action<Entity<MapEditorEyeComponent>>? EyeCreated;

    [ViewVariables(VVAccess.ReadWrite)]
    private bool _readyToMap;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MapEditorUserStateComponent, ComponentStartup>(EditorUserDataStartup);
        SubscribeLocalEvent<MapEditorUserStateComponent, AfterAutoHandleStateEvent>(AfterUserDataState);
        SubscribeLocalEvent<MapEditorEyeComponent, ComponentStartup>(AfterViewStartup);

        SubscribeNetworkEvent<MEM.SaveMapData>(HandleSaveMapData);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_stateManager.CurrentState is MapEditorState)
            _stateManager.RequestStateChange<DefaultState>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_readyToMap)
        {
            _readyToMap = false;
            Log.Info("We're mapping, gamers!");

            _stateManager.RequestStateChange<MapEditorState>();
        }
    }

    private void EditorUserDataStartup(Entity<MapEditorUserStateComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.User != _playerManager.LocalUser)
            return;

        _readyToMap = true;
    }

    private void AfterUserDataState(Entity<MapEditorUserStateComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        OpenMapsUpdated?.Invoke(ent.Comp.OpenMaps);
    }

    private void AfterViewStartup(Entity<MapEditorEyeComponent> ent, ref ComponentStartup args)
    {
        EyeCreated?.Invoke(ent);
    }

    public void RequestStartEditing()
    {
        RaiseNetworkEvent(new MEM.StartEditing());
    }

    public void RequestNewFile()
    {
        RaiseNetworkEvent(new MEM.CreateNewMap());
    }

    public void RequestOpenFile(string name, MapFileHandle handle)
    {
        if (!_mapFileHandles.TryGetStream(handle, out var stream))
            throw new ArgumentException("Invalid handle!", nameof(handle));

        stream.Position = 0;

        var targetStream = new MemoryStream();
        using (var zstdStream = new ZStdCompressStream(targetStream))
        {
            stream.CopyTo(zstdStream);
        }

        RaiseNetworkEvent(new MEM.OpenMap
        {
            Handle = handle,
            Data = targetStream.ToArray(),
            Name = name
        });
    }

    public bool CheckHasMapFileHandle(EntityUid map, out MapFileHandle handle)
    {
        DebugTools.Assert(_playerManager.LocalUser != null);

        var data = Comp<MapEditorMapDataComponent>(map);
        return data.FileHandles.TryGetValue(_playerManager.LocalUser.Value, out handle);
    }

    public void RequestSaveFile(EntityUid map, MapFileHandle handle, string? newName = null)
    {
        RaiseNetworkEvent(new MEM.SaveMap
        {
            MapData = GetNetEntity(map),
            Handle = handle,
            NewName = newName
        });
    }

    public void RequestCreateView(EntityUid map, Vector2 position, MEM.ActionId actionId)
    {
        RaiseNetworkEvent(new MEM.CreateNewView
        {
            Action = actionId,
            MapData = GetNetEntity(map),
            Position = position,
        });
    }

    public void RequestCloseFile(EntityUid map)
    {
        var data = Comp<MapEditorMapDataComponent>(map);
        var handle = data.FileHandles[_playerManager.LocalUser!.Value];

        RaiseNetworkEvent(new MEM.CloseMap
        {
            MapData = GetNetEntity(map)
        });

        _mapFileHandles.CloseHandle(handle);
    }

    private void HandleSaveMapData(MEM.SaveMapData msg)
    {
        Log.Info($"Receiving save map data ({ByteHelpers.FormatBytes(msg.Data.Length)}) for map {msg.Handle}");

        var ms = new MemoryStream(msg.Data, writable: false);
        using var decompression = new ZStdDecompressStream(ms);

        if (!_mapFileHandles.TryGetStream(msg.Handle, out var stream))
        {
            Log.Error($"Received SaveMapData for unknown file handle {msg.Handle}");
            return;
        }

        stream.Position = 0;
        stream.SetLength(0);
        decompression.CopyTo(stream);
        stream.Flush();
    }
}
