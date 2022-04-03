namespace Robust.Client.UserInterface;

public interface IThemeableUI
{
    public UITheme Theme { get; set; }
    public void UpdateTheme(UITheme newTheme);
}
