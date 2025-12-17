using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MultiFPS.Gameplay.Gamemodes
{
    [AddComponentMenu("MultiFPS/Gamemodes/Deathmatch")]
    public class Deathmatch : Gamemode
    {
        public int KillsToWin = 20;

        public Deathmatch()
        {
            Indicator = Gamemodes.Deathmatch;
            LetPlayersSpawnOnTheirOwn = true;
            FFA = true;
            FriendyFire = true; //friendly fire must be true because in free for all Deathmatch everyone are in the same team, 
            //so i they want to fight each other, friendy fire must be true
        }


        public override void PlayerSpawnCharacterRequest(PlayerInstance playerInstance)
        {
            if (LetPlayersSpawnOnTheirOwn)
            {
                playerInstance.Server_SpawnCharacter(defaultSpawnPoints.GetNextSpawnPoint());
            }
        }

        public override void Server_OnPlayerInstanceAdded(PlayerInstance player)
        {
            if (!isServer) return;
            //in FFA we want all players to be in the same team, so we dont let team choose and we choose default team for them instead

            AssignPlayerToTeam(player, 0);
        }

        public override void Server_OnPlayerKilled(Health victimID, Health killerID)
        {
            for (int i = 0; i < GameManager.Players.Count; i++)
            {
                var item = GameManager.Players.ElementAt(i);
                if (item.Kills >= KillsToWin && State == GamemodeState.Inprogress)
                {
                    SwitchGamemodeState(GamemodeState.Finish);
                }
            }
        }

        protected override void TimerEnded()
        {
            base.TimerEnded();

            switch (State)
            {
                case GamemodeState.Warmup:
                    SwitchGamemodeState(GamemodeState.Inprogress);
                    break;

                case GamemodeState.Inprogress:
                    SwitchGamemodeState(GamemodeState.Finish);
                    break;
            }
        }

        protected override void CheckTeamStates()
        {
            base.CheckTeamStates();

            if (!isServer) return;

            //start the game if there is more than 1 player
            if (_teams[0].PlayerInstances.Count > 1)
            {
                if (State == GamemodeState.WaitingForPlayers)
                    SwitchGamemodeState(GamemodeState.Warmup);
            }
            else
            {
                SwitchGamemodeState(GamemodeState.WaitingForPlayers);
            }
        }

        protected override void MatchEvent_StartMatch()
        {
            base.MatchEvent_StartMatch();

            RespawnAllPlayers(defaultSpawnPoints);

            GamemodeMessage("Match started!", 3f);

            CountTimer(GameDuration);

            ResetPlayersStats();

            LetPlayersSpawnOnTheirOwn = true;
        }

        protected override void MatchEvent_EndMatch()
        {
            base.MatchEvent_EndMatch();
            StopTimer();

            BlockAllPlayers(true);

            LetPlayersSpawnOnTheirOwn = false;

            //find the winner
            List<PlayerInstance> players = GameManager.Players;

            players = players.OrderByDescending(x => x.Kills).ToList();

            //display message who won
            GamemodeMessage(players[0].PlayerInfo.Username + " won!", 5f);

            //set timer for next round
            DelaySetGamemodeState(GamemodeState.Warmup, 5f);
        }

        protected override void MatchEvent_StartWarmup()
        {
            base.MatchEvent_StartWarmup();
            LetPlayersSpawnOnTheirOwn = false;
        }
        protected override void OnPlayerAddedToTeam(PlayerInstance player, int team)
        {
            player.Server_SpawnCharacter(defaultSpawnPoints.GetNextSpawnPoint());
        }

        protected override int PlayerRequestToJoinTeamPermission(PlayerInstance player, int requestedTeam)
        {
            return -1;
        }
    }
}