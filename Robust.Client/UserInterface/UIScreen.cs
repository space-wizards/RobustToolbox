using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface;

// ReSharper disable MemberCanBePrivate.Global
[PublicAPI]
public abstract class UIScreen : LayoutContainer
{
    private IConfigurationManager _configManager = IoCManager.Resolve<IConfigurationManager>();

    public Vector2i AutoscaleMaxResolution
    {
        get =>
            new(_configManager.GetCVar<int>("interface.resolutionAutoScaleUpperCutoffX"),
                _configManager.GetCVar<int>("interface.resolutionAutoScaleUpperCutoffY"));
        protected set
        {
            _configManager.SetCVar("interface.resolutionAutoScaleUpperCutoffX", value.X);
            _configManager.SetCVar("interface.resolutionAutoScaleUpperCutoffY", value.Y);
        }
    }

    public Vector2i AutoscaleMinResolution
    {
        get
        {
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            return new Vector2i(configManager.GetCVar<int>("interface.resolutionAutoScaleLowerCutoffX"),
                configManager.GetCVar<int>("interface.resolutionAutoScaleLowerCutoffY"));
        }
        protected set
        {
            _configManager.SetCVar("interface.resolutionAutoScaleLowerCutoffX", value.X);
            _configManager.SetCVar("interface.resolutionAutoScaleLowerCutoffY", value.Y);
        }
    }

    public float AutoscaleFloor
    {
        get
        {
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            return configManager.GetCVar<float>("interface.resolutionAutoScaleMinimum");
        }
        protected set { _configManager.SetCVar("interface.interface.resolutionAutoScaleMinimum", value); }
    }

    private readonly Dictionary<Type, UIWidget> _widgets = new();

    protected UIScreen()
    {
        HorizontalAlignment = HAlignment.Stretch;
        VerticalAlignment = VAlignment.Stretch;
    }

    public T RegisterWidget<T>() where T : UIWidget, new()
    {
        if (_widgets.ContainsKey(typeof(T))) throw new Exception("Hud Widget not found");
        var newWidget = new T();
        return newWidget;
    }

    public void RemoveWidget<T>() where T : UIWidget, new()
    {
        if (_widgets.TryGetValue(typeof(T), out var widget))
        {
            RemoveChild(widget);
        }

        _widgets.Remove(typeof(T));
    }

    internal void OnRemoved()
    {
        OnUnloaded();
    }

    internal void OnAdded()
    {
        OnLoaded();
    }

    public UIWidget? this[Type type]
    {
        get
        {
            if ((type.IsAbstract) || !typeof(UIWidget).IsAssignableFrom(type))
                throw new Exception("Tried to fetch a non UI widget from UI Screen");
            _widgets.TryGetValue(type, out var widget);
            return widget;
        }
    }

    public void AddWidget(UIWidget widget)
    {
        AddChild(widget);
    }

    public T? GetWidget<T>() where T : UIWidget, new()
    {
        return (T?) _widgets.GetValueOrDefault(typeof(T));
    }

    public T GetOrNewWidget<T>() where T : UIWidget, new()
    {
        if (!_widgets.TryGetValue(typeof(T), out var widget))
        {
            widget = new T();
        }

        return (T) widget;
    }

    public bool IsWidgetShown<T>() where T : UIWidget
    {
        return _widgets.TryGetValue(typeof(T), out var widget) && widget.Visible;
    }

    public void ShowWidget<T>(bool show) where T : UIWidget
    {
        _widgets[typeof(T)].Visible = show;
    }

    protected override void ChildAdded(Control newChild)
    {
        base.ChildAdded(newChild);

        RegisterChildren(newChild);

        if (newChild is not UIWidget widget) return;
        if (!_widgets.TryAdd(widget.GetType(), widget))
            throw new Exception("Tried to add duplicate widget to screen!");
    }

    private void RegisterChildren(Control control)
    {
        foreach (var child in control.Children)
        {
            RegisterChildren(child);

            if (child is not UIWidget widget)
            {
                continue;
            }

            if (!_widgets.TryAdd(widget.GetType(), widget))
            {
                throw new Exception("Tried to add duplicate widget to screen!");
            }
        }
    }

    protected override void ChildRemoved(Control child)
    {
        base.ChildRemoved(child);
        RemoveChildren(child);
        if (child is not UIWidget widget) return;
        _widgets.Remove(child.GetType());
    }

    private void RemoveChildren(Control control)
    {
        foreach (var child in control.Children)
        {
            RemoveChildren(child);

            if (child is not UIWidget widget)
            {
                continue;
            }

            _widgets.Remove(widget.GetType());
        }
    }

    protected virtual void OnLoaded()
    {
    }

    protected virtual void OnUnloaded()
    {
    }
}
