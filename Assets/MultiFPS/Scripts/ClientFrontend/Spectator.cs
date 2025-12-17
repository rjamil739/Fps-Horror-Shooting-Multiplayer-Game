using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.UI.HUD;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MultiFPS.UI
{

    public class Spectator : MonoBehaviour
    {

        Coroutine _switchCharacterCoroutine;
        CharacterInstance _observedCharacter;

        private void Awake()
        {
            ClientFrontend.ClientFrontendEvent_OnObservedCharacterSet += OnObserverdCharacterSet;
        }

        private void OnDestroy()
        {
            ClientFrontend.ClientFrontendEvent_OnObservedCharacterSet -= OnObserverdCharacterSet;
        }

        void Update()
        {
            if (!ClientFrontend.ClientPlayerInstance) return;
            if (ClientFrontend.ClientPlayerInstance.Team == -1) return;
            if (ClientFrontend.ClientPlayerInstance.MyCharacter && ClientFrontend.ClientPlayerInstance.MyCharacter.Health.CurrentHealth > 0) return;

            if (Input.GetKeyDown(KeyCode.LeftArrow))
                SwitchTarget(false);
            else if (Input.GetKeyDown(KeyCode.RightArrow))
                SwitchTarget(true);

            if (Input.GetKeyDown(KeyCode.E) && ClientFrontend.OwnedCharacterInstance != ClientFrontend.ObservedCharacterInstance)
            {
                ClientFrontend.ClientPlayerInstance.RequestControlOverPlayerCharacter(ClientFrontend.ObservedCharacterInstance);
            }
        }

        void OnObserverdCharacterSet(CharacterInstance characterInstance)
        {
            StopPlannedCharacterSwitchIfExist();

            if (_observedCharacter)
                _observedCharacter.Health.Client_OnHealthDepleted -= OnObservedCharacterDied;

            _observedCharacter = characterInstance;
            _observedCharacter.Health.Client_OnHealthDepleted += OnObservedCharacterDied;
        }

        /// <summary>
        /// after out observed character died, we would like to switch to another character after couple of secons, this method is
        /// responsible for this
        /// </summary>
        void OnObservedCharacterDied(CharacterPart hittedPartID, byte attackerID)
        {
            StopPlannedCharacterSwitchIfExist();

            _switchCharacterCoroutine = StartCoroutine(SwitchCoroutine(4f));

            IEnumerator SwitchCoroutine(float seconds)
            {
                yield return new WaitForSeconds(seconds);
                SwitchTarget(true);
            }
        }

        void StopPlannedCharacterSwitchIfExist()
        {
            if (_switchCharacterCoroutine != null)
            {
                StopCoroutine(_switchCharacterCoroutine);
                _switchCharacterCoroutine = null;
            }
        }

        void SwitchTarget(bool right)
        {
            List<PlayerInstance> players = GameManager.Players;


            //can occur when we die and immediately return to main menu
            if (players.Count <= 0) return;

            PlayerInstance startCharacter = GameManager.FindPlayerInstanceByCharacter(ClientFrontend.ObservedCharacterInstance);

            int startIndex = players.IndexOf(startCharacter);

            bool foundTarget = false;

            for (int i = startIndex + (right ? 1 : -1); !foundTarget; i += (right ? 1 : -1))
            {
                if (i >= players.Count) i = 0;
                if (i < 0) i = players.Count - 1;

                PlayerInstance pi = players[i];

                if (startCharacter == pi)
                {
                    //No avalaible players to spectate, so return
                    return;
                }

                if (pi.Team != ClientFrontend.ThisClientTeam) continue;
                if (!pi.MyCharacter) continue;
                if (pi.MyCharacter.Health.CurrentHealth <= 0) continue;

                CharacterInstance characterToSpectate = pi.MyCharacter;

                ClientFrontend.SetObservedCharacter(characterToSpectate);
                return;
            }
        }
    }
}