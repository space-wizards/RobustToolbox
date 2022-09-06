using Robust.Client.Graphics;

namespace Robust.Client.UserInterface
{
    //THIS IS BEING DEPRECIATED BECAUSE IT'S ASS!
    //UIThemes will be eventually fully replacing this functionality without giving you turbo space ass-cancer

    /// <summary>
    ///     Fallback theme system for GUI.
    /// </summary>
    public abstract class InterfaceTheme
    {
        public abstract Font DefaultFont { get; }
        public abstract Font LabelFont { get; }
        public abstract StyleBox PanelPanel { get; }
        public abstract StyleBox ButtonStyle { get; }
        public abstract StyleBox LineEditBox { get; }
    }

    public sealed class InterfaceThemeDummy : InterfaceTheme
    {
        public override Font DefaultFont { get; } = new DummyFont();
        public override Font LabelFont { get; } = new DummyFont();
        public override StyleBox PanelPanel { get; } = new StyleBoxFlat();
        public override StyleBox ButtonStyle { get; } = new StyleBoxFlat();
        public override StyleBox LineEditBox { get; } = new StyleBoxFlat();
    }
}
