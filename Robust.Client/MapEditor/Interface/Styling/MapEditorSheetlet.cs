using Robust.Client.Editor.Styling;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Stylesheets;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Robust.Client.MapEditor.Interface.Styling;

[EngineSheetlet]
internal sealed class MapEditorSheetlet : EngineSheetlet<BaseEditorStylesheet>
{
    private const float ToolPreviewSize = 64;

    public override StyleRule[] GetRules(BaseEditorStylesheet sheet, object config)
    {
        return
        [
            Element()
                .Class(MapEditorStyleClasses.ToolPreview)
                .Prop(nameof(Control.SetWidth), ToolPreviewSize)
                .Prop(nameof(Control.SetHeight), ToolPreviewSize)
        ];
    }
}
