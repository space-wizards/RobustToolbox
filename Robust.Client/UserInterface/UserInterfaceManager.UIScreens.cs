using System;
using System.Collections.Generic;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface;

internal partial class UserInterfaceManager
{
    private UIScreen? _activeScreen;

    public UIScreen? ActiveScreen
    {
        get => _activeScreen;
        private set
        {
            if (_activeScreen == value)
                return;

            var old = _activeScreen;
            old?.OnRemoved();
            _activeScreen = value;
            _activeScreen?.OnAdded();

            OnScreenChanged?.Invoke((old, _activeScreen));
        }
    }

    public event Action<(UIScreen? Old, UIScreen? New)>? OnScreenChanged;

    [ViewVariables] public Control ScreenRoot { get; private set; } = default!;

    private readonly Dictionary<Type, UIScreen> _screens = new();

    private void _initializeScreens()
    {
        foreach (var screenType in _reflectionManager.GetAllChildren<UIScreen>())
        {
            if (screenType.IsAbstract) continue;
            _screens.Add(screenType, (UIScreen) _typeFactory.CreateInstance(screenType));
        }

        ScreenRoot = new Control
        {
            Name = "ScreenRoot"
        };
        RootControl.AddChild(ScreenRoot);
        //This MUST be drawn before windowroot
        ScreenRoot.SetPositionInParent(2);
    }

    public void LoadScreen<T>() where T : UIScreen, new()
    {
        ((IUserInterfaceManager) this).LoadScreenInternal(typeof(T));
    }

    public T? GetActiveUIWidgetOrNull<T>() where T : UIWidget, new()
    {
        return (T?) _activeScreen?.GetWidget<T>();
    }

    public T GetActiveUIWidget<T>() where T : UIWidget, new()
    {
        if (_activeScreen == null) throw new Exception("No screen is currently active");
        var widget = _activeScreen.GetWidget<T>();
        if (widget == null) throw new Exception("No widget of type found in active screen");
        return (T) widget;
    }

    void IUserInterfaceManager.LoadScreenInternal(Type type)
    {
        var screen = _screens[type];
        ActiveScreen = screen;
        ScreenRoot.AddChild(screen);
        screen.HorizontalAlignment = Control.HAlignment.Stretch;
        screen.VerticalAlignment = Control.VAlignment.Stretch;
    }

    public void UnloadScreen()
    {
        if (_activeScreen == null) return;
        ScreenRoot.RemoveChild(_activeScreen);
        _activeScreen = null;
    }
}
