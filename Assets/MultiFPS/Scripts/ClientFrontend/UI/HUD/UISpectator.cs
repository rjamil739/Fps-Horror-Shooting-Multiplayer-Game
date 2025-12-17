using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MultiFPS.UI
{
    public class UISpectator : MonoBehaviour
    {
        [SerializeField] Text _usernameTextRenderer;
        [SerializeField] Text _botIndicatorTextRenderer;
        [SerializeField] GameObject _spectatorPanel;

        // Start is called before the first frame update
        void Start()
        {
            ClientFrontend.ClientFrontendEvent_OnObservedCharacterSet += ObserveCharacter;
        }
        private void OnDestroy()
        {
            ClientFrontend.ClientFrontendEvent_OnObservedCharacterSet -= ObserveCharacter;
        }

        public void ObserveCharacter(CharacterInstance characterInstance)
        {
            _spectatorPanel.SetActive(characterInstance != ClientFrontend.OwnedCharacterInstance);

            _usernameTextRenderer.text = "Spectating: " + characterInstance.Health.CharacterName;

            _botIndicatorTextRenderer.enabled = characterInstance.BOT && GameManager.Gamemode.LetPlayersTakeControlOverBots;
        }
    }
}