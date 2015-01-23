using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.GameMode;
using SS14.Server.Interfaces.Player;
using SS14.Shared;
using SS14.Shared.IoC;

namespace SS14.Server.Services.Round
{
    public class Gamemode : IGameMode
    {
        private ISS14Server _server;
        private string description = "";
        private string name = "";

        public Gamemode(ISS14Server server)
        {
            _server = server;
            Name = "Gamemode";
            Description = "This is an empty Gamemode";
        }

        #region IGameMode Members

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        public event GameEndHandler OnGameBegin;
        public event GameUpdateHandler OnGameUpdate;
        public event GameEndHandler OnGameEnd;

        public virtual void SpawnPlayer(IPlayerSession player) //Called by SendMap() after sending everything.
        {
            //This should be handled differently!!!.
            IoCManager.Resolve<IPlayerManager>().SpawnPlayerMob(player);
        }

        public virtual void StartGame() //Called by InitModules() for Game state.
        {
            Begin();
        }

        public virtual void PlayerJoined(IPlayerSession player)
        {
        }

        public virtual void PlayerLeft(IPlayerSession player)
        {
            IoCManager.Resolve<IChatManager>().SendChatMessage(ChatChannel.Server, "Gamemode: Player left!", null,
                                                               player.AttachedEntityUid);
        }

        public virtual void PlayerDied(IPlayerSession player)
        {
            IoCManager.Resolve<IChatManager>().SendChatMessage(ChatChannel.Server, "Gamemode: Player died!", null,
                                                               player.AttachedEntityUid);
        }

        public virtual void Begin()
        {
            if (OnGameBegin != null) OnGameBegin(this);
        }

        public virtual void Update()
        {
            if (OnGameUpdate != null) OnGameUpdate(this);
        }

        public virtual void End()
        {
            if (OnGameEnd != null) OnGameEnd(this);
        }

        #endregion
    }
}