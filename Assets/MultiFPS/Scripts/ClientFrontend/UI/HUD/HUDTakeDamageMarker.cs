using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MultiFPS;
using MultiFPS.Gameplay;

namespace MultiFPS.UI.HUD
{
    public class HUDTakeDamageMarker : MonoBehaviour
    {
        [SerializeField] GameObject markerPrefab;
        List<HUDSingleHitIndicator> takeDamageMarker = new List<HUDSingleHitIndicator>();

        private CharacterInstance _observedCharacterInstance;
        byte maxMarkers = 5;
        byte currentUsedMarkerId = 0;


        private void Awake()
        {
            // GameManager.obser += Initialize;
        }
        private void OnDestroy()
        {
            // GameManager.GameEvent_ObservedCharacterSet -= Initialize;
        }
        void Start()
        {
            takeDamageMarker.Add(markerPrefab.GetComponent<HUDSingleHitIndicator>());
            for (int i = 0; i < maxMarkers - 1; i++)
            {
                takeDamageMarker.Add(Instantiate(markerPrefab, transform).GetComponent<HUDSingleHitIndicator>());
            }
        }
        void Update()
        {
        }
        void SetTakeDamageMarker(int currentHealth, CharacterPart damagedPart, AttackType attackType, byte attackerID)
        {

            Health attacker = GameSync.Singleton.Healths.GetObj(attackerID);


            if (!attacker) return;

            if (currentUsedMarkerId == maxMarkers)
            {
                currentUsedMarkerId = 0;
            }

            takeDamageMarker[currentUsedMarkerId].InitializeIndicator((attacker.transform), _observedCharacterInstance.transform);
            currentUsedMarkerId++;
        }
        public void Initialize(CharacterInstance _charInstance)
        {
            if (_observedCharacterInstance != null) _observedCharacterInstance.Health.Client_OnHealthStateChanged -= SetTakeDamageMarker; //desub previous character
            _observedCharacterInstance = _charInstance;
            _observedCharacterInstance.Health.Client_OnHealthStateChanged += SetTakeDamageMarker;
            foreach (HUDSingleHitIndicator _indicator in takeDamageMarker)
            {
                _indicator.Clear();
            }
        }
    }
}