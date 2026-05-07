using Robust.Client.MapEditor.Interface.Panels;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Client.MapEditor.Tools;

/// <summary>
/// Describes a tool for placing a specified entity prototype.
/// </summary>
/// <seealso cref="MapEditorToolEntitySystem"/>
[RegisterComponent]
[Access(typeof(MapEditorToolEntitySystem))]
internal sealed partial class MapEditorToolEntityComponent : Component
{
    [DataField]
    public EntProtoId PrototypeId;
}

// That's "map editor" "tool entity" "system"

/// <summary>
/// Implements <see cref="MapEditorToolEntityComponent"/>.
/// </summary>
internal sealed class MapEditorToolEntitySystem : EntitySystem
{
    [Dependency] private readonly ClientMapEditorSystem _mapEditor = null!;
    [Dependency] private readonly IPrototypeManager _prototype = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MapEditorToolEntityComponent, MapEditorToolValidateEvent>(ValidateTool);
        SubscribeLocalEvent<MapEditorToolEntityComponent, MapEditorToolMakePreviewControl>(MakePreviewControl);
    }

    public void SwitchToTool(EntityUid mapData, EntProtoId protoId)
    {
        _mapEditor.SwitchToTool(mapData,
            toolEnt => { AddComp<MapEditorToolEntityComponent>(toolEnt).PrototypeId = protoId; });
    }

    private void ValidateTool(Entity<MapEditorToolEntityComponent> ent, ref MapEditorToolValidateEvent args)
    {
        var protoId = ent.Comp.PrototypeId;
        if (!_prototype.TryIndex(protoId, out var prototype))
        {
            Log.Warning($"Failed to validate tool: entity prototype '{protoId}' does not exist.");
            return;
        }

        if (!IsEligible(prototype))
        {
            Log.Warning($"Failed to validate tool: entity prototype '{protoId}' is not spawnable.");
            return;
        }

        args.Name = EntityPickerButton.FormatName(prototype);
        args.MakeValid();
    }

    private static void MakePreviewControl(
        Entity<MapEditorToolEntityComponent> ent,
        ref MapEditorToolMakePreviewControl args)
    {
        var view = new EntityPrototypeView();
        view.SetPrototype(ent.Comp.PrototypeId);
        args.SetControl(view);
    }

    internal static bool IsEligible(EntityPrototype prototype)
    {
        return !prototype.Abstract && !prototype.Abstract;
    }
}
