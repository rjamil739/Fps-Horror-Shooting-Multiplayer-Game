
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MultiFPS.Gameplay.Gamemodes
{
    public class RangeGamemode : Gamemode
    {
        [SerializeField] SpawnpointsContainer _enemyTeamSpawnPoints;

        protected override void OnPlayerAddedToTeam(PlayerInstance player, int team)
        {
            if (team == 0)
                player.Server_SpawnCharacter(defaultSpawnPoints.GetNextSpawnPoint());
            else
                player.Server_SpawnCharacter(_enemyTeamSpawnPoints.GetNextSpawnPoint());
        }

        public override void PlayerSpawnCharacterRequest(PlayerInstance player)
        {
            if (player.Team == 0)
                player.Server_SpawnCharacter(defaultSpawnPoints.GetNextSpawnPoint());
            else
                player.Server_SpawnCharacter(_enemyTeamSpawnPoints.GetNextSpawnPoint());
        }
    }
}