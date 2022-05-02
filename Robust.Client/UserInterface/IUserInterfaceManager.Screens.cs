using System;

namespace Robust.Client.UserInterface;

public partial interface IUserInterfaceManager
{
    public UIScreen? ActiveScreen { get;}
    public void LoadScreen<T>() where T : UIScreen, new();
    internal void LoadScreenInternal(Type type);
    public void UnloadScreen();

}
