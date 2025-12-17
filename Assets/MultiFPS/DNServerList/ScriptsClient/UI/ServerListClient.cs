using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace DNServerList
{
    [RequireComponent(typeof(WebRequestManager))]
    public class ServerListClient : MonoBehaviour
    {
        public static ServerListClient Singleton { get; set; }

        [Header("Server list")]
        public UnityEvent <string, LobbyData[], int, int> OnServerListReceived;
        public UnityEvent<string, ushort> OnCreatedLobbySuccesfully;
        public UnityEvent OnLobbyCouldNotBeCreated;
        public UnityEvent OnConnectionError;

        [Header("Lobby codes")]
        public UnityEvent<string, ushort> OnLobbyFoundByCode;
        public UnityEvent OnLobbyNotFound;

        [Header("QuickPlay")]
        public UnityEvent<string, ushort> OnQuickPlayFound;
        public UnityEvent OnQuickPlayError;

        WebRequestManager _webRequestManager;

        private void OnEnable()
        {
            if (Singleton)
            {
                
                Destroy(Singleton.gameObject);
            }

            Singleton = this;

            _webRequestManager = gameObject.GetComponent<WebRequestManager>();
        }
        private void OnDisable()
        {
            Singleton = null;
        }

        public void GetServerList()
        {
            _webRequestManager.Get($"{DNRegionManager.Sigleton.SelectedRegion.Url}/matchmaking/getserverlist", OnSuccess, OnError);

            void OnSuccess(string data, int code)
            {
                print(data);

                Lobbies ReceivedLobbiesJson = JsonUtility.FromJson<Lobbies>(data);

                OnServerListReceived?.Invoke(ReceivedLobbiesJson.address, ReceivedLobbiesJson.lobbies, 
                    System.Convert.ToInt32(ReceivedLobbiesJson.thisLobbiesStartingIndex),
                    System.Convert.ToInt32(ReceivedLobbiesJson.totalLobbiesCount));
            }

            void OnError(string data, int code)
            {
                OnConnectionError?.Invoke();
            }
        }

        public void GetServerByCode(string code)
        {
            string url = $"{DNRegionManager.Sigleton.SelectedRegion.Url}/matchmaking/getprivategame/{code}";
            _webRequestManager.Get(url, OnSuccess, (string data, int code) => OnConnectionError?.Invoke());

            void OnSuccess(string data, int code)
            {
                if (code != 202) return;

                PlayerConnectToRoomRequest connectInfo = JsonUtility.FromJson<PlayerConnectToRoomRequest>(data);

                if (!string.IsNullOrEmpty(connectInfo.address))
                    OnLobbyFoundByCode?.Invoke(connectInfo.address, System.Convert.ToUInt16(connectInfo.port));
                else
                    OnLobbyNotFound?.Invoke();
            }
            
        }

        public void SendCreateLobbyRequest<T>(T gameProperties, bool isPrivate)
        {
            CreateGameContract form = new ();
            form.metadata = JsonUtility.ToJson(gameProperties);
            form.isPrivate = isPrivate;

            string finalForm = JsonUtility.ToJson(form);

            _webRequestManager.PostJson($"{DNRegionManager.Sigleton.SelectedRegion.Url}/matchmaking/createpublicgame", finalForm, OnSuccess, OnError);

            void OnSuccess(string data, int code)
            {

                if (code == 202)
                {
                    PlayerConnectToRoomRequest connectInfo = JsonUtility.FromJson<PlayerConnectToRoomRequest>(data);
                    OnCreatedLobbySuccesfully?.Invoke(connectInfo.address, System.Convert.ToUInt16(connectInfo.port));
                }
                else
                    OnLobbyCouldNotBeCreated?.Invoke();
            }

            void OnError(string data, int code)
            {
                OnConnectionError?.Invoke();
            }
        }

        public void SendQuickPlayRequest()
        {
            _webRequestManager.Get($"{DNRegionManager.Sigleton.SelectedRegion.Url}/matchmaking/quickplay", OnSuccess, OnError);

            void OnSuccess(string data, int code)
            {
                if (code == 202)
                {
                    PlayerConnectToRoomRequest connectInfo = JsonUtility.FromJson<PlayerConnectToRoomRequest>(data);
                    OnQuickPlayFound?.Invoke(connectInfo.address, System.Convert.ToUInt16(connectInfo.port));
                }
                else
                    OnQuickPlayError?.Invoke();
            }

            void OnError(string data, int code)
            {
                OnQuickPlayError?.Invoke();
            }
        }
    }

    [System.Serializable]
    public class Lobbies
    {
        public LobbyData[] lobbies;
        public int thisLobbiesStartingIndex;
        public int totalLobbiesCount;
        public string address;
    }

    [System.Serializable]
    public class LobbyData
    {
        public string metadata;
        public string accessPort;
    }

    struct CreateGameContract
    {
        public string metadata;
        public bool isPrivate;
    }

    [System.Serializable]
    public class PlayerConnectToRoomRequest
    {
        public string address;
        public ushort port;
    }
}