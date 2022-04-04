using System.Collections.Generic;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Robust.Client.UserInterface.Themes;

public interface IUIThemeManager
{
    internal void Initialize();
    public UITheme CurrentTheme { get;}
    public UITheme this[string name] { get; }
    public UITheme GetTheme(string name);
    public UITheme GetThemeOrDefault(string name);
    public void SetActiveTheme(string themeName);
}


public sealed class UiThemeManager : IUIThemeManager
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IUserInterfaceManagerInternal _userInterfaceManager = default!;

    private readonly Dictionary<string, UITheme> _themes = new();

    public UITheme CurrentTheme { get; private set; } = default!;

    public UITheme DefaultTheme => _protoManager.Index<UITheme>(UITheme.DefaultName);

    void IUIThemeManager.Initialize()
    {
        CurrentTheme = DefaultTheme;
        foreach (var proto in _protoManager.EnumeratePrototypes<UITheme>())
        {
            _themes.Add(proto.ID, proto);
        }
        _configurationManager.OnValueChanged(CVars.InterfaceTheme, SetThemeOrPrevious, true);
    }
    //Try to set the current theme, if the theme is not found do nothing
    public void SetActiveTheme(string themeName)
    {
        if (!_themes.TryGetValue(themeName, out var theme) || (theme == CurrentTheme)) return;
        CurrentTheme = theme;
    }

    private void UpdateTheme(UITheme newTheme)
    {
        if (newTheme == CurrentTheme) return; //do not update if the theme is unchanged
        CurrentTheme = newTheme;
        _userInterfaceManager.RootControl.ThemeUpdateRecursive();
    }

    //Try to set the current theme, if the theme is not found leave the previous theme
    public void SetThemeOrPrevious(string name)
    {
        UpdateTheme(GetThemeOrCurrent(name));
    }

    //Try to set the current theme, if the theme is not found set the default theme
    public void SetThemeOrDefault(string name)
    {
        UpdateTheme(GetThemeOrDefault(name));
    }

    public UITheme GetThemeOrCurrent(string name)
    {
        return !_themes.TryGetValue(name, out var theme) ? CurrentTheme : theme;
    }

    public UITheme GetThemeOrDefault(string name)
    {
        return !_themes.TryGetValue(name, out var theme) ? DefaultTheme : theme;
    }

    public UITheme GetTheme(string name)
    {
        return _themes[name];
    }

    public UITheme this[string name] => GetTheme(name);
}
