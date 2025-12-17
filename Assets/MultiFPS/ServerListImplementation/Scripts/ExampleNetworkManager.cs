using DNServerList;
using Mirror;
using Mirror.SimpleWeb;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiFPS.ServerList
{
    public class ExampleNetworkManager : NetworkManager
    {

        public delegate void OnPlayerCountChanged(NetworkConnectionToClient conn);

        public static OnPlayerCountChanged OnPlayerConnected;
        public static OnPlayerCountChanged OnPlayerDisconnected;

        public override void OnServerConnect(NetworkConnectionToClient conn) => OnPlayerConnected?.Invoke(conn);

        public override void OnServerDisconnect(NetworkConnectionToClient conn) => OnPlayerDisconnected?.Invoke(conn);

        public void ClientUseWss(bool use)
        {
            GetComponent<SimpleWebTransport>().clientUseWss = use;
        }
    }
}