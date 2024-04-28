using System.Collections.Generic;
using Robust.Client.UserInterface.Themes;
using Robust.Shared;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

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
        ReloadThemes();
        _configurationManager.OnValueChanged(CVars.InterfaceTheme, SetThemeOrPrevious, true);
        _protoManager.PrototypesReloaded += OnPrototypesReloaded;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs eventArgs)
    {
        if (eventArgs.WasModified<UITheme>())
        {
            _sawmillUI.Debug("Reloading UI themes due to prototype reload");
            ReloadThemes();
        }
    }

    private void ReloadThemes()
    {
        _themes.Clear();
        foreach (var proto in _protoManager.EnumeratePrototypes<UITheme>())
        {
            _themes.Add(proto.ID, proto);
        }

        SetThemeOrPrevious(CurrentTheme.ID);
    }

    //Try to set the current theme, if the theme is not found do nothing
    public void SetActiveTheme(string themeName)
    {
        if (!_themes.TryGetValue(themeName, out var theme) || (theme == CurrentTheme)) return;
        UpdateTheme(theme);
    }

    public void SetDefaultTheme(string themeId)
    {
        if (_defaultOverriden)
        {
            //this exists to stop people from misusing default theme
            _sawmillUI.Error("Tried to set default theme twice!");
            return;
        }

        if (!_protoManager.TryIndex(themeId, out UITheme? theme))
        {
            _sawmillUI.Error("Could not find UI theme prototype for ID:"+ themeId);
            return;
        }
        DefaultTheme = theme;
        UpdateTheme(theme);
        _defaultOverriden = true;
    }

    private void UpdateTheme(UITheme newTheme)
    {
        if (newTheme == CurrentTheme)
            return;

        CurrentTheme = newTheme;
        _userInterfaceManager.RootControl.ThemeUpdateRecursive(CurrentTheme);
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
