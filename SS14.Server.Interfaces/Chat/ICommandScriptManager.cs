using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Server.Interfaces.Chat
{
    public interface ICommandScriptManager
    {
        void RunFunction(string script); // TODO add parameter functionality.
    }
}
