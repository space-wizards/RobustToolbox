using System;
using System.Collections.Generic;
using Robust.Client.UserInterface.Controls;

namespace Robust.Client.UserInterface;

public abstract class UIScreen : LayoutContainer
{
    private readonly Dictionary<System.Type, UIWidget> _widgets = new();
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
}
