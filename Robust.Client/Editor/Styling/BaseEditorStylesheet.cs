using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Stylesheets;
using Robust.Shared.Maths;

namespace Robust.Client.Editor.Styling;

public abstract class BaseEditorStylesheet : CommonEngineStylesheet
{
    private protected BaseEditorStylesheet(object config) : base(config)
    {
        var rules = (StyleRule[])
        [
            ..GetFontRules(),
            ..GetAllSheetletRules<BaseEditorStylesheet, EngineSheetletAttribute>()
        ];

        Stylesheet = new Stylesheet(rules);
    }

    internal abstract Color BaseBackground { get; }
    internal abstract Color ButtonBackground { get; }
    internal abstract Color ButtonBackgroundHover { get; }

    internal abstract Color BaseAccent { get; }
}
