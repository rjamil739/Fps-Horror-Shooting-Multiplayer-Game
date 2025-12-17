using MultiFPS;
using MultiFPS.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiFPS
{
    [RequireComponent(typeof(Health))]
    public class BotsPeacefulnessSwitch : MonoBehaviour
    {
        void Start() 
        {
            GetComponent<Health>().Server_OnDamaged += Server_OnDamaged;
        }
        void Server_OnDamaged(int currentHealth, CharacterPart damagedPart, AttackType attackType, Health attacker, int attackForce)
        {
            GameManager.Gamemode.PeacefulBots = !GameManager.Gamemode.PeacefulBots;
        }
    }
}