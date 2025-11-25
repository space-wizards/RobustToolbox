using Robust.Shared.Maths;

namespace Robust.Client.Editor.Styling;

public sealed class EditorDarkStylesheet : BaseEditorStylesheet
{
    public const string Name = "EditorDark";

    internal EditorDarkStylesheet(object config) : base(config)
    {
    }

    internal override Color LowBackground => Color.FromHex("#140D1A");
    internal override Color BaseBackground => Color.FromHex("#1C1029");
    internal override Color HighBackground => Color.FromHex("#231436");
    internal override Color ButtonBackground => Color.FromHex("#3F0B50");
    internal override Color ButtonBackgroundHover => Color.FromHex("#540E6B");

    internal override Color BaseAccent => Color.FromHex("#0AA6BD");
}
