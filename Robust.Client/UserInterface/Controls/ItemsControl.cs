using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Robust.Client.UserInterface.Controls;

[Virtual]
public class ItemsControl : Control, IVirtualizingContainerParent
{
    private static readonly NotifyCollectionChangedEventArgs NotifyReset = new(NotifyCollectionChangedAction.Reset);

    private IList _items = Array.Empty<object>();
    private Control _panelControl = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
    private NotifyCollectionChangedEventHandler? _collectionChanged;
    private IControlTemplate? _itemTemplate;

    public Control PanelControl
    {
        get => _panelControl;
        set
        {
            if (_panelControl == value)
                return;

            if (value is { Parent: not null })
                throw new ArgumentException("Assigned panel must not be attached to a control.");

            {
                _panelControl.Orphan();
                if (_panelControl is IVirtualizingContainer virt)
                    virt.ClearParent();
            }

            _panelControl = value;

            {
                AddChild(_panelControl);
                if (_panelControl is IVirtualizingContainer virt)
                {
                    virt.SetParent(this);
                }
                else
                {
                    // Trigger rebuild logic on new control.
                    ItemsOnCollectionChanged(this, NotifyReset);
                }
            }
        }
    }

    public IControlTemplate? ItemTemplate
    {
        get => _itemTemplate;
        set => _itemTemplate = value;
    }

    public IEnumerable ItemsSource
    {
        set
        {
            if (_items is INotifyCollectionChanged notifyOld)
                notifyOld.CollectionChanged -= ItemsOnCollectionChanged;

            _items = value switch
            {
                IList list => list,
                IEnumerable<object> enumerable => new List<object>(enumerable),
                _ => new List<object>(value.Cast<object>())
            };

            if (_items is INotifyCollectionChanged notifyNew)
                notifyNew.CollectionChanged += ItemsOnCollectionChanged;

            ItemsOnCollectionChanged(this, NotifyReset);
        }
    }

    public ItemsControl()
    {
        AddChild(_panelControl);
    }

    private void ItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _collectionChanged?.Invoke(sender, e);

        if (_panelControl is null or IVirtualizingContainer)
            return;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                AddItems(e.NewStartingIndex, e.NewItems!);
                break;
            case NotifyCollectionChangedAction.Remove:
                RemoveItems(e.OldStartingIndex, e.OldItems!.Count);
                break;
            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Move:
                // TODO: More intelligent move handling is possible here.
                RemoveItems(e.OldStartingIndex, e.OldItems!.Count);
                AddItems(e.NewStartingIndex, e.NewItems!);
                break;
            case NotifyCollectionChangedAction.Reset:
                _panelControl.RemoveAllChildren();
                AddItems(0, _items);
                break;
        }

        return;

        void AddItems(int startIndex, IList items)
        {
            var insertIndex = startIndex;
            foreach (var item in items)
            {
                var control = CreateControl(item);
                _panelControl.AddChild(control);
                if (insertIndex != _panelControl.ChildCount)
                    control.SetPositionInParent(insertIndex);
                insertIndex += 1;
            }
        }

        void RemoveItems(int startIndex, int itemCount)
        {
            for (var i = 0; i < itemCount; i++)
            {
                _panelControl.RemoveChild(startIndex);
            }
        }
    }

    int IVirtualizingContainerParent.ItemCount => _items.Count;

    private Control CreateControl(object? item)
    {
        if (_itemTemplate == null)
        {
            // Fallback if no template is provided.
            var str = item?.ToString() ?? "";
            return new Label { Text = str };
        }

        return _itemTemplate.Instantiate(item);
    }

    Control IVirtualizingContainerParent.CreateControl(object? item)
    {
        return CreateControl(item);
    }

    event NotifyCollectionChangedEventHandler IVirtualizingContainerParent.CollectionChanged
    {
        add => _collectionChanged += value;
        remove => _collectionChanged -= value;
    }
}
