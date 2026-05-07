using Robust.Client.Editor.Styling;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Stylesheets;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Robust.Client.MapEditor.Interface.Styling;

[EngineSheetlet]
internal sealed class MapEditorSheetlet : EngineSheetlet<BaseEditorStylesheet>
{
    public override StyleRule[] GetRules(BaseEditorStylesheet sheet, object config)
    {
        return
        [
            Element()
                .Class(MapEditorStyleClasses.ToolHistoryEntry)
                .Prop(nameof(Control.Margin), new Thickness(2))
                .Prop(nameof(Control.SetWidth), 32)
                .Prop(nameof(Control.SetHeight), 32)

        ];
    }
}
