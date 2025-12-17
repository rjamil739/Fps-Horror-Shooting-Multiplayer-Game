using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using MultiFPS.Gameplay;
using System;

namespace MultiFPS
{
    /// <summary>
    /// This class adds frequently used functionality in this package to base networkbahaviour class in mirror.
    /// It contains void "OnNewPlayerConnected" launched ON SERVER everytime someone connects to the game. Thanks to that we can
    /// update object state for that client, for example: health and equipment of other players that are already spawned when someone
    /// connected
    /// </summary>
    public class DNNetworkBehaviour : NetworkBehaviour
    {
        public byte DNID { private set; get; }

        bool _subscribedToNetworkManager = false;
        protected virtual void OnNewPlayerConnected(NetworkConnection conn) { }

        protected virtual void OnEnable()
        {
            if (!_subscribedToNetworkManager && DNNetworkManager.Instance)
            {
                DNNetworkManager.Instance.OnNewPlayerConnected += OnNewPlayerConnected;
                _subscribedToNetworkManager = true;
            }
        }
        protected virtual void OnDisable()
        {
            if(_subscribedToNetworkManager)
                DNNetworkManager.Instance.OnNewPlayerConnected -= OnNewPlayerConnected;
        }

        internal void SetDNID(byte id)
        {
            DNID = id;
        }
    }
}