using Robust.Client.Graphics;

namespace Robust.Client.UserInterface
{
    // DON'T USE THESE
    // THEY'RE A BAD IDEA THAT NEEDS TO BE BURIED.

    /// <summary>
    ///     Fallback theme system for GUI.
    /// </summary>
    public abstract class UITheme
    {
        public abstract IFontLibrary DefaultFontLibrary { get; }
        public abstract IFontLibrary LabelFontLibrary { get; }
        public Font DefaultFont { get => DefaultFontLibrary.StartFont().Current; }
        public Font LabelFont { get => LabelFontLibrary.StartFont().Current; }
        public abstract StyleBox PanelPanel { get; }
        public abstract StyleBox ButtonStyle { get; }
        public abstract StyleBox LineEditBox { get; }
    }


    public sealed class UIThemeDummy : UITheme
    {
        private static readonly FontClass _defaultFontClass = new FontClass ( Id: "dummy", Size: default, Style: default );
        public override IFontLibrary DefaultFontLibrary { get; } = new FontLibrary(_defaultFontClass);
        public override IFontLibrary LabelFontLibrary { get; } = new FontLibrary(_defaultFontClass);
        public override StyleBox PanelPanel { get; } = new StyleBoxFlat();
        public override StyleBox ButtonStyle { get; } = new StyleBoxFlat();
        public override StyleBox LineEditBox { get; } = new StyleBoxFlat();

        public UIThemeDummy() : base()
        {
            DefaultFontLibrary.AddFont("dummy",
                new []
                {
                        new DummyVariant (default)
                }
            );
            LabelFontLibrary.AddFont("dummy",
                new []
                {
                    new DummyVariant (default)
                }
            );
        }
    }
}
