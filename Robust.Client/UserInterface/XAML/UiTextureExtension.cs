using JetBrains.Annotations;
using Robust.Client.UserInterface.Themes;
using Robust.Shared.IoC;

namespace Robust.Client.UserInterface.XAML;

[PublicAPI]
public sealed class UiTexExtension
{
    public string Path { get; }
    public UITheme Theme { get; }
    public UiTexExtension(string path)
    {
        Path = path;
        Theme = IoCManager.Resolve<IUserInterfaceManager>().CurrentTheme;
    }
    //Support for forcing a theme
    public UiTexExtension(UITheme theme, string path)
    {
        Path = path;
        Theme = theme;
    }

    public object ProvideValue()
    {
        return Theme.ResolveTexture(Path);
    }
}
