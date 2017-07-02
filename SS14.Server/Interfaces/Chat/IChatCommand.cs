using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.Command;
using SS14.Shared.IoC;
using SS14.Shared.Network;

namespace SS14.Server.Interfaces.Chat
{
    public interface IChatCommand : ICommand
    {
        void Execute(IChatManager manager, NetChannel client, params string[] args);
    }
}
