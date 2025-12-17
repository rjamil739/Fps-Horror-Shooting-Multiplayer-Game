using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiFPS.UI.HUD
{
    public class HUDItem : MonoBehaviour
    {
        [SerializeField]
        GameObject _scopeHud;

        private void Start()
        {
            if(_scopeHud)
                _scopeHud.SetActive(false);
        }

        public virtual void Scope(bool scope)
        {
            if(_scopeHud)
                _scopeHud.SetActive(scope);
        }

    }
}