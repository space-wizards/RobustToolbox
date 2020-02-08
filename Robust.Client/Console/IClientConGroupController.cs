using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robust.Client.Console
{
    public interface IClientConGroupController
    {
        void Initialize();

        bool CanCommand(string cmdName);
        bool CanViewVar();
        bool CanAdminPlace();
        event Action ConGroupUpdated;
    }
}
