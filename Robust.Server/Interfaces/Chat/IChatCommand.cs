using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Robust.Server.Interfaces.Chat
{
    public interface IChatCommand : ICommand
    {
        void Execute(IChatManager manager, INetChannel client, params string[] args);
    }
}
