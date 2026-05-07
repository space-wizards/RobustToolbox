using System;
using System.Collections.Specialized;
using Robust.Shared.Utility;
using LayoutOrientation = Robust.Client.UserInterface.Controls.BoxContainer.LayoutOrientation;

namespace Robust.Client.UserInterface.Controls;

public sealed class VirtualizingBoxContainer : Control, IVirtualizingContainer
{
    private LayoutOrientation _orientation;
    private IVirtualizingContainerParent? _parent;

    public LayoutOrientation Orientation
    {
        get => _orientation;
        set
        {
            _orientation = value;
            InvalidateMeasure();
        }
    }

    void IVirtualizingContainer.SetParent(IVirtualizingContainerParent parent)
    {
        DebugTools.Assert(_parent == null);

        _parent = parent;
        _parent.CollectionChanged += ParentOnCollectionChanged;
    }

    void IVirtualizingContainer.ClearParent()
    {
        DebugTools.Assert(_parent != null);

        _parent.CollectionChanged -= ParentOnCollectionChanged;
        _parent = null;
    }

    private void ParentOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        throw new NotImplementedException();
    }
}
