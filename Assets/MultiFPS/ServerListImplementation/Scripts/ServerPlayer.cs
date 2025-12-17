using DNServerList;
using Mirror;
using Mirror.SimpleWeb;
using MultiFPS;
using MultiFPS.Gameplay.Gamemodes;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace MultiFPS.ServerList
{
    public class ServerPlayer : MonoBehaviour
    {

        [SerializeField] GameSettingsSO _gameSettings;

#if !UNITY_WEBGL
        ExampleLobbyProperties _thisLobbyProperties;
        int currentConnectedPlayers = 0;

#endif

        public void ServeQuickPlayLobby(ushort port)
        {
            Debug.LogError($"Serving quickplay {port}");
#if !UNITY_WEBGL
            DNNetworkManager.Instance.OnNewPlayerConnected += OnPlayerCountChanged;
            DNNetworkManager.Instance.OnPlayerDisconnected += OnPlayerCountChanged;

            int randomMapID = UnityEngine.Random.Range(1, _gameSettings.Maps.Length);
            MapRepresenter map = _gameSettings.Maps[randomMapID];

            ExampleCreateLobbyForm form = new ExampleCreateLobbyForm
            {
                mapID = randomMapID,
            };

            NetworkManager networkManager = NetworkManager.singleton;

            networkManager.offlineScene = SceneUtility.GetScenePathByBuildIndex(0);

            networkManager.onlineScene = map.Scene;


            RoomSetup.Properties.P_Gamemode = map.AvailableGamemodes[form.gamemodeID];
            RoomSetup.Properties.P_FillEmptySlotsWithBots = true;
            RoomSetup.Properties.P_GameDuration = Mathf.FloorToInt(_gameSettings.GameDurations[_gameSettings.GameDurations.Length - 1] * 60);

            int maxPlayers = map.MaxPlayersPresets[map.MaxPlayersPresets.Length - 1];


            RoomSetup.Properties.P_MaxPlayers = maxPlayers;
            networkManager.maxConnections = maxPlayers;

            RoomSetup.Properties.P_RespawnCooldown = 6f;


            networkManager.GetComponent<SimpleWebTransport>().port = port;
            networkManager.StartServer();

            ExampleLobbyProperties lobbyProperties = new ExampleLobbyProperties
            {
                ServerName = $"Lobby{UnityEngine.Random.Range(0, 1000)}",
                MapID = form.mapID,
                GamemodeID = form.gamemodeID,
                MaxPlayers = map.MaxPlayersPresets.Length - 1,
                CurrentPlayers = currentConnectedPlayers,
            };

            _thisLobbyProperties = lobbyProperties;

            Req_UpdatePublicLobbyProperties post = new Req_UpdatePublicLobbyProperties
            {
                CurrentConnections = 0,
                MaxConnections = (byte)maxPlayers,
                Metadata = JsonUtility.ToJson(_thisLobbyProperties),
            };

            ServerCommunicator.Singleton.OnGameReady(JsonUtility.ToJson(post));
#endif
        }

        public void StartServer(ushort port, string formInJson)
        {

#if !UNITY_WEBGL
            DNNetworkManager.Instance.OnNewPlayerConnected += OnPlayerCountChanged;
            DNNetworkManager.Instance.OnPlayerDisconnected += OnPlayerCountChanged;

            ExampleCreateLobbyForm form = JsonUtility.FromJson<ExampleCreateLobbyForm>(formInJson);

            NetworkManager networkManager = NetworkManager.singleton;

            networkManager.offlineScene = SceneUtility.GetScenePathByBuildIndex(0);

            networkManager.onlineScene = _gameSettings.Maps[System.Convert.ToInt32(form.mapID)].Scene;


            RoomSetup.Properties.P_Gamemode = _gameSettings.Maps[form.mapID].AvailableGamemodes[form.gamemodeID];
            RoomSetup.Properties.P_FillEmptySlotsWithBots = form.spawnBots > 0;
            RoomSetup.Properties.P_GameDuration = Mathf.FloorToInt(_gameSettings.GameDurations[form.gameDuration] * 60);

            int maxPlayers = 2;

            if (form.maxPlayers < _gameSettings.Maps[form.mapID].MaxPlayersPresets.Length)
                maxPlayers = _gameSettings.Maps[form.mapID].MaxPlayersPresets[form.maxPlayers];
            else
            {
                Debug.LogWarning($"Player number count index out of range, index: {form.maxPlayers}, size:{_gameSettings.Maps[form.mapID].MaxPlayersPresets.Length} ");
            }

            RoomSetup.Properties.P_MaxPlayers = maxPlayers;
            networkManager.maxConnections = maxPlayers;

            RoomSetup.Properties.P_RespawnCooldown = 6f;


            networkManager.GetComponent<SimpleWebTransport>().port = port;
            networkManager.StartServer();

            ExampleLobbyProperties lobbyProperties = new ExampleLobbyProperties
            {
                ServerName = string.IsNullOrEmpty(form.serverName) ? $"Lobby{UnityEngine.Random.Range(0, 1000)}" : form.serverName,
                MapID = form.mapID,
                GamemodeID = form.gamemodeID,
                MaxPlayers = form.maxPlayers,
                CurrentPlayers = currentConnectedPlayers,
            };

            _thisLobbyProperties = lobbyProperties;

            Req_UpdatePublicLobbyProperties post = new Req_UpdatePublicLobbyProperties
            {
                CurrentConnections = 0,
                MaxConnections = (byte)maxPlayers,
                Metadata = JsonUtility.ToJson(_thisLobbyProperties),
            };

            ServerCommunicator.Singleton.OnGameReady(JsonUtility.ToJson(post));
#endif
        }

        [System.Serializable]
        public class Req_UpdatePublicLobbyProperties
        {
            public string Metadata;
            public byte CurrentConnections;
            public byte MaxConnections;
        }

#if !UNITY_WEBGL
        void OnPlayerCountChanged(NetworkConnectionToClient conn)
        {
            _thisLobbyProperties.CurrentPlayers = NetworkServer.connections.Count;

            Req_UpdatePublicLobbyProperties post = new Req_UpdatePublicLobbyProperties
            {
                CurrentConnections = (byte)NetworkServer.connections.Count,
                MaxConnections = (byte)RoomSetup.Properties.P_MaxPlayers,
                Metadata = JsonUtility.ToJson(_thisLobbyProperties),
            };

            ServerCommunicator.Singleton.OnPlayerCountChanged(NetworkServer.connections.Count, JsonUtility.ToJson(post));
        }
#endif
    }
}