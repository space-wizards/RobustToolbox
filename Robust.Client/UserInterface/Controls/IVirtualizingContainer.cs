using System.Collections.Specialized;

namespace Robust.Client.UserInterface.Controls;

public interface IVirtualizingContainer
{
    void SetParent(IVirtualizingContainerParent parent);
    void ClearParent();
}

public interface IVirtualizingContainerParent
{
    int ItemCount { get; }
    Control CreateControl(object? item);
    event NotifyCollectionChangedEventHandler CollectionChanged;
}
