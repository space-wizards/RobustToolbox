namespace SS14.Server.Interfaces.Console
{
    public interface ISystemConsoleManager
    {
        void Update();
        void Initialize();

        void Print(string text);
    }
}
