using System.Collections.Generic;
using Robust.Client.UserInterface.Themes;
using Robust.Shared;
using Robust.Shared.Log;

namespace Robust.Client.UserInterface;

internal partial class UserInterfaceManager
{
    private readonly Dictionary<string, UITheme> _themes = new();

    public UITheme CurrentTheme { get; private set; } = default!;

    private bool _defaultOverriden = false;
    public UITheme DefaultTheme { get; private set; } = default!;

    private void _initThemes()
    {
        DefaultTheme = _protoManager.Index<UITheme>(UITheme.DefaultName);
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

    public void SetDefaultTheme(string themeId)
    {
        if (_defaultOverriden)
        {
            //this exists to stop people from misusing default theme
            Logger.Error("Tried to set default theme twice!");
            return;
        }

        if (!_protoManager.TryIndex(themeId, out UITheme? theme))
        {
            Logger.Error("Could not find UI theme prototype for ID:"+ themeId);
            return;
        }
        DefaultTheme = theme;
        UpdateTheme(theme);
        _defaultOverriden = true;
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

}
