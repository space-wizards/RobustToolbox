namespace Robust.Server.Console
{
    internal sealed class SystemConsoleManagerDummy : ISystemConsoleManager
    {
        public void UpdateInput()
        {
            // Nada.
        }

        public void Print(string text)
        {
            // Nada.
        }

        public void UpdateTick()
        {
            // Nada.
        }
    }
}
