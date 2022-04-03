using JetBrains.Annotations;

namespace Robust.Client.UserInterface.XAML;

[PublicAPI]
public sealed class UiTexExtension
{
    public string Path { get; }
    public UITheme Theme { get; }
    public UiTexExtension(string path)
    {
        Path = path;
        Theme = UITheme.Default;
    }

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
