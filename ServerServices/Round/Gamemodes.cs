using SS13.IoC;
using SS13_Shared;
using ServerInterfaces;
using ServerInterfaces.Chat;
using ServerInterfaces.GameMode;
using ServerInterfaces.Player;

namespace ServerServices.Round
{
    public class Gamemode : IGameMode
    {
        private ISS13Server _server;
        private string description = "";
        private string name = "";

        public Gamemode(ISS13Server server)
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
                                                               player.attachedEntity.Uid);
        }

        public virtual void PlayerDied(IPlayerSession player)
        {
            IoCManager.Resolve<IChatManager>().SendChatMessage(ChatChannel.Server, "Gamemode: Player died!", null,
                                                               player.attachedEntity.Uid);
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