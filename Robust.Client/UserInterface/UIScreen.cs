using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;

namespace Robust.Client.UserInterface;


// ReSharper disable MemberCanBePrivate.Global
[PublicAPI]
public abstract class UIScreen : LayoutContainer
{
    private readonly Dictionary<Type, UIWidget> _widgets = new();
    protected UIScreen()
    {
        HorizontalAlignment = HAlignment.Stretch;
        VerticalAlignment = VAlignment.Stretch;
    }

    public T RegisterWidget<T>() where T: UIWidget, new()
    {
        if (_widgets.ContainsKey(typeof(T))) throw new Exception("Hud Widget not found");
        var newWidget = new T();
        AddUIWidget(newWidget);
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
    public UIWidget? GetWidget<T>() where T : UIWidget, new()
    {
        return _widgets.GetValueOrDefault(typeof(T));
    }
    public UIWidget GetOrNewWidget<T>() where T : UIWidget, new()
    {
        if (!_widgets.TryGetValue(typeof(T), out var widget))
        {
            widget = new T();
        }
        return widget;
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
        if (newChild is not UIWidget widget) return;
        if (!_widgets.TryAdd(widget.GetType(), widget))
            throw new Exception("Tried to add duplicate widget to screen!");
    }

    protected override void ChildRemoved(Control child)
    {
        base.ChildRemoved(child);
        if (child is not UIWidget widget) return;
        _widgets.Remove(child.GetType());
    }
    protected void OnLoaded() {}

    protected void OnUnloaded() {}

}
