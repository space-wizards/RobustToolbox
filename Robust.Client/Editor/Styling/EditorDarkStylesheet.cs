using Robust.Shared.Maths;

namespace Robust.Client.Editor.Styling;

public sealed class EditorDarkStylesheet : BaseEditorStylesheet
{
    public const string Name = "EditorDark";

    internal EditorDarkStylesheet(object config) : base(config)
    {
    }

    internal override Color BaseBackground => Color.FromHex("#181818");
}
