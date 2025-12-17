using MultiFPS.Gameplay;

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using MultiFPS.UI;
namespace MultiFPS.UI.HUD
{
    public class HUDDamagePopup : CharacterHud
    {
        public static HUDDamagePopup Instance;

        [SerializeField] HUDDamagePopupElement _popupPrefab;
        [SerializeField] int _maxPopupsAtOnce = 6;
        [SerializeField] float _liveTime;
        public VerticalLayoutGroup GridParent;

        HUDDamagePopupElement[] _popups;

        int _nextPopupIDtoUse = 0;

        uint _lastVictimID = 0;

        [HideInInspector] public HUDDamagePopupElement _currentElement;

        [SerializeField] bool _separatePopupForElimination = true;

        protected override void Awake()
        {
            base.Awake();

            _popupPrefab.Init(this, _liveTime);
            _popupPrefab.gameObject.SetActive(false);

            _popups = new HUDDamagePopupElement[_maxPopupsAtOnce];
            for (int i = 0; i < _maxPopupsAtOnce - 1; i++)
            {
                HUDDamagePopupElement popup = Instantiate(_popupPrefab.gameObject, GridParent.transform).GetComponent<HUDDamagePopupElement>();
                _popups.SetValue(popup, i);
                popup.Init(this, _liveTime);
            }

            _popups.SetValue(_popupPrefab.GetComponent<HUDDamagePopupElement>(), _maxPopupsAtOnce - 1);

            Instance = this;
        }


        public void UpdateState(int currentHealth, int takenDamage, CharacterPart damagedPart, AttackType attackType, byte victimID)
        {
            if (victimID == _myObservedCharacter.DNID && takenDamage > 0) //dont display damage popup for self harm 
                return;

            //if one of those conditions is met use another element instead of one at the top
            if (victimID != _lastVictimID || takenDamage<0 && _separatePopupForElimination || _currentElement == null)
            {
                _lastVictimID = victimID;

                _nextPopupIDtoUse++;
                if (_nextPopupIDtoUse >= _popups.Length)
                    _nextPopupIDtoUse = 0;

                _currentElement = _popups[_nextPopupIDtoUse];

                _currentElement.ResetPopup();
            }
            _currentElement.transform.SetAsFirstSibling();
            _currentElement.Set(takenDamage, victimID);
        }
        void Killed(CharacterPart damagedPart, byte victimID) 
        {
            if (victimID == _myObservedCharacter.DNID) return; //dont display popup for self inflicted damage

            UpdateState(0, -1, damagedPart, AttackType.hitscan, victimID);
        }

        protected override void AssignCharacterForUI(CharacterInstance _characterInstanceToAssignForUI)
        {
            _characterInstanceToAssignForUI.Health.Client_OnDamageDealt += UpdateState;
            _characterInstanceToAssignForUI.Health.Client_KillConfirmation += Killed;
        }
        protected override void DeassignCurrentCharacterFromUI(CharacterInstance _characterToDeassign)
        {
            _characterToDeassign.Health.Client_OnDamageDealt -= UpdateState;
            _characterToDeassign.Health.Client_KillConfirmation -= Killed;
        }
    }
}