using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SS13_Server.Modules.Gamemodes;
using SS13_Server.Modules;
using SS13_Server;
using ServerInterfaces;
using ServerInterfaces.Player;
using SS13.IoC;
using ServerInterfaces.GameMode;

namespace SS13_Server.Modules.Gamemodes
{
    public class Gamemode : IGameMode
    {
        private string name = "";
        public string Name { get { return name; } set { name = value; } }

        private string description = "";
        public string Description { get { return description; } set { description = value; } }

        public event GameEndHandler OnGameBegin;
        public event GameUpdateHandler OnGameUpdate;
        public event GameEndHandler OnGameEnd;      

        private SS13Server server;

        public Gamemode()
        {
            server = SS13_Server.SS13Server.Singleton;
            Name = "Gamemode";
            Description = "This is an empty Gamemode";
        }

        public virtual void SpawnPlayer(IPlayerSession player) //Called by SendMap() after sending everything.
        {                                                     //This should be handled differently!!!.
            IoCManager.Resolve<IPlayerManager>().SpawnPlayerMob(player);
        }

        public virtual void StartGame() //Called by InitModules() for Game state.
        {
            Begin();
        }

        public virtual void PlayerJoined(IPlayerSession player)
        {
        }

        public virtual void PlayerLeft(IPlayerSession player) //Not Called right now
        {
        }

        public virtual void PlayerDied(IPlayerSession player) //Not Called right now
        {
        }

        public virtual void Begin()
        {
            if(OnGameBegin != null) OnGameBegin(this);
        }
        public virtual void Update()
        {
            if (OnGameUpdate != null) OnGameUpdate(this);
        }

        public virtual void End()
        {
            if (OnGameEnd != null) OnGameEnd(this);
        }
    }
}
