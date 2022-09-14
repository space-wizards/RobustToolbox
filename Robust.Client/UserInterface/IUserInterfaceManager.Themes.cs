using Robust.Client.UserInterface.Themes;

namespace Robust.Client.UserInterface;

public partial interface IUserInterfaceManager
{
    public UITheme CurrentTheme { get;}
    public UITheme GetTheme(string name);
    public UITheme GetThemeOrDefault(string name);
    public void SetActiveTheme(string themeName);
    public UITheme DefaultTheme { get; }
    public void SetDefaultTheme(string themeId);
}
