namespace Robust.Client.UserInterface;

public interface IUIScreenManager
{

}
public sealed class UIScreenManager : IUIScreenManager
{
    public UIScreen? ActiveScreen { get; private set;}

    public void Initialize()
    {

    }
}
