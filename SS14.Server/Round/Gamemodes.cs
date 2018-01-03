using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.GameMode;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Console;
using SS14.Shared.IoC;

namespace SS14.Server.Round
{
    public class Gamemode : IGameMode
    {
        private IBaseServer _server;
        private string description = "";
        private string name = "";

        public Gamemode(IBaseServer server)
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
            IoCManager.Resolve<IChatManager>().DispatchMessage(ChatChannel.Server, "Gamemode: Player left!", player.Index);
        }

        public virtual void PlayerDied(IPlayerSession player)
        {
            IoCManager.Resolve<IChatManager>().DispatchMessage(ChatChannel.Server, "Gamemode: Player died!", player.Index);
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
