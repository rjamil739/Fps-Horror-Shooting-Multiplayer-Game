using MultiFPS.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MultiFPS.UI.HUD
{
    public class UICharacterNametag : UIWorldSpaceElement
    {
        [SerializeField] Text _usernameRenderer;
        [SerializeField] Image _usernameRenderer_frame;
        [SerializeField] Image _healthBarRenderer;
        [SerializeField] Image _markerRenderer;
       // [SerializeField] ContentBackground _textBackground;

        CharacterInstance _myCharInstance;

        bool _spawned;


        public void Set(CharacterInstance characterInstance)
        {
            _myCharInstance = characterInstance;

            _usernameRenderer.text = _myCharInstance.Health.CharacterName;

            Color color = ClientInterfaceManager.Instance.UIColorSet.AppropriateColorAccordingToTeam(_myCharInstance.Health.Team);

            _usernameRenderer.color = color;
            _usernameRenderer_frame.color = color;
            _healthBarRenderer.color = color;
            _markerRenderer.color = color;

            SetObjectToFollow(_myCharInstance.CharacterMarkerPosition);

            _myCharInstance.Health.Client_OnHealthDepleted += OnMyCharacterDeath;
            _myCharInstance.Health.Client_OnHealthStateChanged += OnMyCharacterDamaged;
            _myCharInstance.Client_OnDestroyed += DespawnMe;

            _spawned = true;

            _healthBarRenderer.fillAmount = (float)_myCharInstance.Health.CurrentHealth / _myCharInstance.Health.MaxHealth;
        }

        void OnMyCharacterDamaged(int currentHealth, CharacterPart hittedPartID, AttackType attackType, byte attackerID)
        {
            _healthBarRenderer.fillAmount = (float)currentHealth / _myCharInstance.Health.MaxHealth;
        }

        void OnMyCharacterDeath(CharacterPart hittedPartID, byte attackerID)
        {
            DespawnMe();
        }

        public void DespawnMe() 
        {
            if (!_spawned) return;

            _spawned = false;
            _myCharInstance.Health.Client_OnHealthDepleted -= OnMyCharacterDeath;
            _myCharInstance.Health.Client_OnHealthStateChanged -= OnMyCharacterDamaged;
            _myCharInstance.Client_OnDestroyed -= DespawnMe;
            Destroy(gameObject);
        }
        private void OnDestroy()
        {
            _spawned = false;
        }
    }
}
