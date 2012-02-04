using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SS13_Server.Modules.Gamemodes;
using SS13_Server.Modules;
using SS13_Server;

namespace SS13_Server.Modules.Gamemodes
{
    public delegate void GameBeginHandler(IGameMode origin);
    public delegate void GameUpdateHandler(IGameMode origin);
    public delegate void GameEndHandler(IGameMode origin);

    public interface IGameMode
    {
        string Name { get; set; }
        string Description { get; set; }

        event GameEndHandler OnGameBegin;    //Raised when the Game begins.
        event GameUpdateHandler OnGameUpdate;//Raised when the Game updates.
        event GameEndHandler OnGameEnd;      //Raised when the Game ends.

        void PlayerJoined(PlayerSession player); //Called when a player joins the round.
        void PlayerLeft(PlayerSession player);   //Called when a player leaves the round.
        void PlayerDied(PlayerSession player);   //Called when a player dies.

        void StartGame();    //Starts the gamemode. But not the actual round.
                             //StartGame() -> WarmUp time or something -> OnGameBegin.

        void SpawnPlayer(PlayerSession player); //Spawn a Player.

        void Begin();        //Called after StartGame(). Raises OnGameBegin. Assign objectives etc here.
        void Update();       //Called regulary. Raises OnGameUpdate. Update Gamemode logic here.
        void End();          //Called when round ends. Raises OnGameEnd. Should be called from the logic in Update().
    }

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

        public virtual void SpawnPlayer(PlayerSession player) //Called by SendMap() after sending everything.
        {                                                     //This should be handled differently!!!.
            server.playerManager.SpawnPlayerMob(player);
        }

        public virtual void StartGame() //Called by InitModules() for Game state.
        {
            Begin();
        }

        public virtual void PlayerJoined(PlayerSession player)
        {
        }

        public virtual void PlayerLeft(PlayerSession player) //Not Called right now
        {
        }

        public virtual void PlayerDied(PlayerSession player) //Not Called right now
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
