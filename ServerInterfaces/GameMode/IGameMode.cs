using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServerInterfaces.Player;

namespace ServerInterfaces.GameMode
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

        void PlayerJoined(IPlayerSession player); //Called when a player joins the round.
        void PlayerLeft(IPlayerSession player);   //Called when a player leaves the round.
        void PlayerDied(IPlayerSession player);   //Called when a player dies.

        void StartGame();    //Starts the gamemode. But not the actual round.
        //StartGame() -> WarmUp time or something -> OnGameBegin.

        void SpawnPlayer(IPlayerSession player); //Spawn a Player.

        void Begin();        //Called after StartGame(). Raises OnGameBegin. Assign objectives etc here.
        void Update();       //Called regulary. Raises OnGameUpdate. Update Gamemode logic here.
        void End();          //Called when round ends. Raises OnGameEnd. Should be called from the logic in Update().
    }
}
