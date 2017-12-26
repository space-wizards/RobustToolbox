using System;
using System.Linq;
using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Console;
using SS14.Shared.IoC;

namespace SS14.Server.ClientConsoleHost.Commands
{
    class SayCommand : IClientCommand
    {
        public string Command => "say";
        public string Description => "Send chat messages to the local channel or a specified radio channel.";
        public string Help => "say [<:channel>] <text>";

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            //TODO: parse channel and broadcast message.
        }
    }

    class WhisperCommand : IClientCommand
    {
        public string Command => "whisper";
        public string Description => "Send chat messages to the local channel in a 1 meter radius.";
        public string Help => "whisper <text>";

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            throw new NotImplementedException();
        }
    }

    class MeCommand : IClientCommand
    {
        public string Command => "me";
        public string Description => "Send third person chat messages to the local channel.";
        public string Help => "me <text>";

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            // clients format the i/they
            var chat = IoCManager.Resolve<IChatManager>();
            chat.DispatchMessage(ChatChannel.Emote, args[0], player.Index);
        }
    }

    class OocCommand : IClientCommand
    {
        public string Command => "ooc";
        public string Description => "Send Out of Character chat messages.";
        public string Help => "ooc <text>";

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            var chat = IoCManager.Resolve<IChatManager>();
            chat.DispatchMessage(ChatChannel.OOC, args[0], player.Index);
        }
    }
}
