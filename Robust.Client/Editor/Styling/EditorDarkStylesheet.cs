using Robust.Shared.Maths;

namespace Robust.Client.Editor.Styling;

public sealed class EditorDarkStylesheet : BaseEditorStylesheet
{
    public const string Name = "EditorDark";

    internal EditorDarkStylesheet(object config) : base(config)
    {
    }

    internal override Color BaseBackground => Color.FromHex("#181818");
    internal override Color ButtonBackground => Color.FromHex("#3D3D3D");
    internal override Color ButtonBackgroundHover => Color.FromHex("#484848");

    internal override Color BaseAccent => Color.FromHex("#0072FF");
}
