using System.Linq;
using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.Player;
using SS14.Shared;
using SS14.Shared.Console;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;

namespace SS14.Server.ClientConsoleHost.Commands
{
    internal class SayCommand : IClientCommand
    {
        private const char RadioChar = ':'; // first char of first argument to designate radio messages
        private const int VoiceRange = 7; // how far voice goes in world units

        public string Command => "say";
        public string Description => "Send chat messages to the local channel or a specified radio channel.";
        public string Help => "say [<:channel>] <text>";

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            if (player.Status != SessionStatus.InGame || !player.AttachedEntityUid.HasValue)
                return;

            if (args.Length < 1)
                return;

            var sessions = IoCManager.Resolve<IPlayerManager>();
            var ents = IoCManager.Resolve<IEntityManager>();
            var chat = IoCManager.Resolve<IChatManager>();

            var message = args[0];

            string text;
            if (message[0] == RadioChar)
            {
                // all they sent was the channel
                if (args.Length < 2)
                    return;

                var channel = args[0];
                var listArgs = args.ToList();
                listArgs.RemoveAt(0);
                text = string.Concat(listArgs);

                //TODO: Parse channel and broadcast over radio.
            }
            else
            {
                text = string.Concat(args);
            }

            var pos = ents.GetEntity(player.AttachedEntityUid.Value).GetComponent<ITransformComponent>().LocalPosition;
            var clients = sessions.GetPlayersInRange(pos, VoiceRange).Select(p => p.ConnectedClient);

            chat.DispatchMessage(clients.ToList(), ChatChannel.Local, text, player.Index);
        }
    }

    internal class WhisperCommand : IClientCommand
    {
        private const int WhisperRange = 1; // how far voice goes in world units

        public string Command => "whisper";
        public string Description => "Send chat messages to the local channel in a 1 meter radius.";
        public string Help => "whisper <text>";

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            if (player.Status != SessionStatus.InGame || !player.AttachedEntityUid.HasValue)
                return;

            var sessions = IoCManager.Resolve<IPlayerManager>();
            var ents = IoCManager.Resolve<IEntityManager>();
            var chat = IoCManager.Resolve<IChatManager>();

            var pos = ents.GetEntity(player.AttachedEntityUid.Value).GetComponent<ITransformComponent>().LocalPosition;
            var clients = sessions.GetPlayersInRange(pos, WhisperRange).Select(p => p.ConnectedClient);

            chat.DispatchMessage(clients.ToList(), ChatChannel.Local, args[0], player.Index);
        }
    }

    internal class MeCommand : IClientCommand
    {
        private const int VoiceRange = 7;

        public string Command => "me";
        public string Description => "Send third person chat messages to the local channel.";
        public string Help => "me <text>";

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            if (player.Status != SessionStatus.InGame || !player.AttachedEntityUid.HasValue)
                return;

            var sessions = IoCManager.Resolve<IPlayerManager>();
            var ents = IoCManager.Resolve<IEntityManager>();
            var chat = IoCManager.Resolve<IChatManager>();

            if (chat.ExpandEmote(args[0], player, out var self, out var other))
            {
                //TODO: Dispatch in PVS range instead
                var pos = ents.GetEntity(player.AttachedEntityUid.Value).GetComponent<ITransformComponent>().LocalPosition;
                var clients = sessions.GetPlayersInRange(pos, VoiceRange).Where(p => p != player).Select(p => p.ConnectedClient);

                chat.DispatchMessage(player.ConnectedClient, ChatChannel.Emote, self, player.Index);
                chat.DispatchMessage(clients.ToList(), ChatChannel.Emote, other, player.Index);
            }
            else
            {
                //TODO: Dispatch in PVS range instead
                var pos = ents.GetEntity(player.AttachedEntityUid.Value).GetComponent<ITransformComponent>().LocalPosition;
                var clients = sessions.GetPlayersInRange(pos, VoiceRange).Select(p => p.ConnectedClient);

                chat.DispatchMessage(clients.ToList(), ChatChannel.Emote, $"{player.Name} {args[0]}", player.Index);
            }
        }
    }

    internal class OocCommand : IClientCommand
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
