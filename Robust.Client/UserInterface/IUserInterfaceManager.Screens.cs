using System;
using Robust.Client.UserInterface.Controls;

namespace Robust.Client.UserInterface;

public partial interface IUserInterfaceManager
{
    public UIScreen? ActiveScreen { get; }
    public void LoadScreen<T>() where T : UIScreen, new();
    internal void LoadScreenInternal(Type type);
    public void UnloadScreen();
    public T? GetActiveUIWidgetOrNull<T>() where T : UIWidget, new();
    public T GetActiveUIWidget<T>() where T : UIWidget, new();
}
