using DNServerList;
using DNServerList.UI;
using Mirror;
using Mirror.SimpleWeb;
using MultiFPS;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MultiFPS.ServerList
{
    public class LobbyTile : UIServerListLobbyRepresenter
    {
        [SerializeField] Text _roomName;
        [SerializeField] Text _gamemodeName;
        [SerializeField] Text _mapName;
        [SerializeField] Text _playerCount;

        [SerializeField] Button _connectButton;

        [SerializeField] GameSettingsSO _gameSettings;

        [SerializeField] UILoadingScreen _loadingScreen;

        public override void Draw(string lobbyData)
        {
            ExampleLobbyProperties data = JsonUtility.FromJson<ExampleLobbyProperties>(lobbyData);

            MultiFPS.MapRepresenter map = _gameSettings.Maps[data.MapID];

            _mapName.text = map.Name;

            _roomName.text = data.ServerName;

            _gamemodeName.text = (map.AvailableGamemodes != null && map.AvailableGamemodes.Length > 0) ?
                map.AvailableGamemodes[data.GamemodeID].ToString() :
                string.Empty;

            _playerCount.text = map.MaxPlayersPresets != null && map.MaxPlayersPresets.Length > 0 ?
                $"{data.CurrentPlayers}/{map.MaxPlayersPresets[data.MaxPlayers]}" :
                data.CurrentPlayers.ToString();

            _connectButton.onClick.AddListener(ButtonConnect);
        }

        void ButtonConnect()
        {
            _loadingScreen.ShowLoadingScreen("Connecting to the game...", 5f);
            StartCoroutine(Connect());
            IEnumerator Connect()
            {
                yield return new WaitForEndOfFrame();
                DNNetworkManager networkManager = DNNetworkManager.Instance;

                networkManager.networkAddress = ServerAddress;
                networkManager.Action_SetNetworkManagerPort?.Invoke(Port);
                networkManager.StartClient();
            }
        }
    }
}