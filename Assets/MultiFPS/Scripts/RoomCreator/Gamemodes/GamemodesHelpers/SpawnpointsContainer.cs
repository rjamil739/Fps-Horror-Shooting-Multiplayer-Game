using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiFPS.Gameplay.Gamemodes {
    public class SpawnpointsContainer : MonoBehaviour
    {
        [HideInInspector] public int _lastUsedSpawnpointID;
        public List<Transform> Spawnpoints;

        private void Awake()
        {
            _lastUsedSpawnpointID = Random.Range(0, Spawnpoints.Count);
        }

        public Transform GetNextSpawnPoint()
        {
            if (Spawnpoints == null || Spawnpoints.Count <= 0)
            {
                print("MultiFPS: No spawnpoints assigned in this map, using ROOMMANAGER gameobject as spawnpoint.");
                return transform;
            }

            if (_lastUsedSpawnpointID >=Spawnpoints.Count)
                _lastUsedSpawnpointID = 0;

            Transform nextSpawnPoint = Spawnpoints[_lastUsedSpawnpointID];

            _lastUsedSpawnpointID++;

            if (nextSpawnPoint == null)
            {
                print("MultiFps Spawner fatal error, couldn't find spawnpoint");
            }

            return nextSpawnPoint;
        }

        public Transform GetBestSpawnPoint(int team)
        {
            //searching for best spawn point
            if (Spawnpoints.Count <= 0)
            {
                print("NO SPAWNPOINTS ASSIGNED IN GAMEMODE");
                return null;
            }

            Transform bestSpawnPoint = Spawnpoints[Random.Range(0, Spawnpoints.Count)];

            float bestDistance = 0;
            foreach (Transform spawnPoint in Spawnpoints)
            {
                float nearestEnemyDistance = float.MaxValue;

                foreach (CharacterInstance character in GameSync.Singleton.Characters.Obj)
                {
                    if(character == null) continue;

                    if (character.Health.Team != team || GameManager.Gamemode.FFA)
                    {
                        float currentCalculatedDistance = Vector3.Distance(character.transform.position, spawnPoint.position);
                        if (currentCalculatedDistance < nearestEnemyDistance)
                        {
                            nearestEnemyDistance = currentCalculatedDistance;
                        }
                    }
                }

                if (nearestEnemyDistance > bestDistance)
                {
                    bestSpawnPoint = spawnPoint;
                    bestDistance = nearestEnemyDistance;
                }
            }
            return bestSpawnPoint;
        }
    }
}