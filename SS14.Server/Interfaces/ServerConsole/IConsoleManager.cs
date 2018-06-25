using System.Collections.Generic;
using SS14.Shared.IoC;

namespace SS14.Server.Interfaces.ServerConsole
{
    public interface IConsoleManager
    {
        void Update();
        void Initialize();

        void Print(string text);
    }
}
