using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MultiFPS.Gameplay;
using UnityEngine.UI;
namespace MultiFPS.UI.HUD
{
    public class PlayerNametag : UIWorldIcon
    {
        public Text namePlaceholder;
        public Image healthbar;

        CharacterInstance myCharacter;

        public void SetupNameplate(CharacterInstance _myCharacter) 
        {
            _myCharacter.Health.Client_OnHealthStateChanged += OnPlayerHealthStateChanged;
            myCharacter = _myCharacter;

            namePlaceholder.text = myCharacter.Health.CharacterName;

            InitializeWorldIcon(myCharacter.CharacterMarkerPosition, false);
        }
        void OnPlayerHealthStateChanged(int currentHealth, CharacterPart damagedPart, AttackType attackType, byte attackerID) 
        {
            healthbar.fillAmount = (float)currentHealth/myCharacter.Health.MaxHealth;
        }

        private void OnDestroy()
        {
            myCharacter.Health.Client_OnHealthStateChanged -= OnPlayerHealthStateChanged;
        }

    }
}