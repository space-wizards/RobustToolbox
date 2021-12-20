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
        public abstract Font DefaultFont { get; }
        public abstract Font LabelFont { get; }
        public abstract StyleBox PanelPanel { get; }
        public abstract StyleBox ButtonStyle { get; }
        public abstract StyleBox LineEditBox { get; }
    }

    public sealed class UIThemeDummy : UITheme
    {
        public override Font DefaultFont { get; } = new DummyFont();
        public override Font LabelFont { get; } = new DummyFont();
        public override StyleBox PanelPanel { get; } = new StyleBoxFlat();
        public override StyleBox ButtonStyle { get; } = new StyleBoxFlat();
        public override StyleBox LineEditBox { get; } = new StyleBoxFlat();
    }
}
