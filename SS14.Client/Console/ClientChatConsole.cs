using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Console
{
    /// <summary>
    ///     Expands the console to support chat, channels, and emotes.
    /// </summary>
    public class ClientChatConsole : ClientConsole, IClientChatConsole
    {
        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
        }
    }
}
