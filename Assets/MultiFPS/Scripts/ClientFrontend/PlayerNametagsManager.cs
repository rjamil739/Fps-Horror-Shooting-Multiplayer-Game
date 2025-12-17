using MultiFPS.Gameplay;
using MultiFPS.UI.HUD;
using MultiFPS.UI;
using MultiFPS;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerNametagsManager : MonoBehaviour
{
    public GameObject PlayerNametagPrefab;
    List<UICharacterNametag> _spawnedNametags = new();

    void Awake()
    {
        GameManager.GameEvent_CharacterTeamAssigned += OnCharacterTeamAssigned;
        ClientFrontend.ClientFrontendEvent_OnObservedCharacterSet += OnObservedCharacterSet;
    }

    //reassign nametags when spectated player is changed
    public void OnObservedCharacterSet(CharacterInstance characterInstance)
    {
        DespawnAllNametags();

        List<PlayerInstance> players = GameManager.Players;

        for (int i = 0; i < players.Count; i++)
        {
            OnCharacterTeamAssigned(players[i].MyCharacter);
        }
    }

    public void OnCharacterTeamAssigned(CharacterInstance characterInstance)
    {
        if (!characterInstance) return;
        //dont spawn nametag for player if we dont know yet which team our player belongs to
        if (!ClientFrontend.ClientTeamAssigned) return;

        //dont spawn matkers for enemies
        if (ClientFrontend.ThisClientTeam != characterInstance.Health.Team || GameManager.Gamemode.FFA) return;

        if (characterInstance.Health.CurrentHealth <= 0) return;
        //dont spawn nametag for player who views world from first person perspective

        if (characterInstance.netId == ClientFrontend.ObservedCharacterNetID())
            return;

        UICharacterNametag playerNameTag = Instantiate(PlayerNametagPrefab).GetComponent<UICharacterNametag>();
        playerNameTag.Set(characterInstance);

        _spawnedNametags.Add(playerNameTag);
    }

    void DespawnAllNametags()
    {
        for (int i = 0; i < _spawnedNametags.Count; i++)
        {
            _spawnedNametags[i].DespawnMe();
        }
        _spawnedNametags.Clear();
    }
}
