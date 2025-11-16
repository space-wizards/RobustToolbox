using Robust.Shared.Maths;

namespace Robust.Client.Editor.Styling;

internal sealed class EditorLightStylesheet : BaseEditorStylesheet
{
    public const string Name = "EditorLight";

    internal EditorLightStylesheet(object config) : base(config)
    {
    }

    internal override Color BaseBackground => Color.FromHex("#EEE");
}
