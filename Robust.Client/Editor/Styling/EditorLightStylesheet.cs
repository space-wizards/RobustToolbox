using Robust.Shared.Maths;

namespace Robust.Client.Editor.Styling;

internal sealed class EditorLightStylesheet : BaseEditorStylesheet
{
    public const string Name = "EditorLight";

    internal EditorLightStylesheet(object config) : base(config)
    {
    }

    internal override Color LowBackground => default;
    internal override Color BaseBackground => Color.FromHex("#EEE");
    internal override Color HighBackground => default;
    internal override Color ButtonBackground => default;
    internal override Color ButtonBackgroundHover => default;
    internal override Color BaseAccent => default;
}
