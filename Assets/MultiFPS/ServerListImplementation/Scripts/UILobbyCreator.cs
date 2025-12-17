using DNServerList.Example;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DNServerList;
using Mirror;
using Mirror.SimpleWeb;
using MultiFPS;
namespace MultiFPS.ServerList
{
    public class UILobbyCreator : MonoBehaviour
    {
        [SerializeField] UIPanelsManager _panelsManager;
        [SerializeField] GameSettingsSO _gameSettings;


        [SerializeField] Dropdown GamemodeSelectionDropdown;
        [SerializeField] Dropdown GameDurationDropdown;
        [SerializeField] Dropdown PlayerNumberDropdown;


        [SerializeField] InputField LobbyNameIF;
        [SerializeField] Button ServeGameButton;
        [SerializeField] Toggle _isPrivate;
        [SerializeField] Toggle _spawnBots;

        [Header("Map selection")]
        [SerializeField] Image _selectedMapIcon;
        [SerializeField] Text _selectedMapName;


        [SerializeField] Transform _mapListGridParent;
        [SerializeField] GameObject _mapTilePrefab;

        //user input
        int _selectedMapID;
        int _selectedTimeDurationID;
        int _selectedPlayerNumberOptionID;
        int _selectedGamemodeID;

        [SerializeField] UILoadingScreen _loadingScreen;

        public delegate void OnMapSelected(int mapID);
        public OnMapSelected Event_OnMapSelected;

        void Start()
        {
            List<string> mapOptions = new List<string>();

            for (int i = 0; i < _gameSettings.Maps.Length; i++)
            {
                UILobbyCreatorMapTile tile = Instantiate(_mapTilePrefab, _mapListGridParent.transform).GetComponent<UILobbyCreatorMapTile>();
                tile.Setup(this, i, _gameSettings.Maps[i]);
            }

            _mapTilePrefab.SetActive(false);


            GamemodeSelectionDropdown.onValueChanged.AddListener(OnGamemodeSelected);
            GameDurationDropdown.onValueChanged.AddListener(OnGameDurationSelected);
            PlayerNumberDropdown.onValueChanged.AddListener(OnPlayerNumberOption);

            //game duration options
            List<string> durationOptions = new List<string>();

            for (int i = 0; i < _gameSettings.GameDurations.Length; i++)
            {
                durationOptions.Add(_gameSettings.GameDurations[i].ToString() + " minutes");
            }

            GameDurationDropdown.AddOptions(durationOptions);



            if (ServeGameButton) ServeGameButton.onClick.AddListener(SendCreateLobbyRequest);

            if (_gameSettings.Maps.Length > 0)
                SelectMap(0);
        }

        void SendCreateLobbyRequest()
        {
            ExampleCreateLobbyForm form = new ExampleCreateLobbyForm
            {
                mapID = _selectedMapID,
                gamemodeID = _selectedGamemodeID,
                gameDuration = _selectedTimeDurationID,
                maxPlayers = _selectedPlayerNumberOptionID,
                serverName = LobbyNameIF.text,
                spawnBots = _spawnBots.isOn ? 1 : 0,
            };

            ServerListClient.Singleton.SendCreateLobbyRequest(form, _isPrivate.isOn);


            _loadingScreen.ShowLoadingScreen("Creating game...", 10f);
        }

        public void SelectMap(int mapID)
        {
            _selectedMapID = mapID;



            //draw gamemode dropdown


            MapRepresenter map = _gameSettings.Maps[mapID];

            _selectedMapIcon.sprite = map.Icon;
            _selectedMapName.text = map.Name;

            List<string> options = new List<string>();
            for (int i = 0; i < map.AvailableGamemodes.Length; i++)
            {
                options.Add(map.AvailableGamemodes[i].ToString());
            }

            GamemodeSelectionDropdown.ClearOptions();
            GamemodeSelectionDropdown.AddOptions(options);

            //draw maxPlayerNumber dropdown
            List<string> playerNumberOptions = new List<string>();
            //player number options
            for (int i = 0; i < map.MaxPlayersPresets.Length; i++)
            {
                playerNumberOptions.Add(map.MaxPlayersPresets[i].ToString() + " players");
            }
            PlayerNumberDropdown.ClearOptions();
            PlayerNumberDropdown.AddOptions(playerNumberOptions);

            Event_OnMapSelected?.Invoke(mapID);
        }

        void OnGamemodeSelected(int gamemodeID) //gamemode ID is relevant to gamemodes order in their enum
        {
            _selectedGamemodeID = gamemodeID;
        }
        void OnGameDurationSelected(int timeOptionID)
        {
            _selectedTimeDurationID = timeOptionID;
        }
        void OnPlayerNumberOption(int playerOptionID)
        {
            _selectedPlayerNumberOptionID = playerOptionID;
        }

        public void OnLobbyCreated(string address, ushort port)
        {

            _loadingScreen.ShowLoadingScreen("Lobby created, connecting...", 7f);

            StartCoroutine(Connect());
            IEnumerator Connect()
            {

                yield return new WaitForEndOfFrame();
                yield return new WaitForSeconds(0.2f);
                DNNetworkManager networkManager = DNNetworkManager.Instance;

                networkManager.networkAddress = address;
                networkManager.Action_SetNetworkManagerPort(port);
                networkManager.StartClient();
            }
        }

        public void OnLobbyCouldNotBeCreated()
        {
            _loadingScreen.HideLoadingScreen();
            _loadingScreen.ShowMessageScreen("Lobby could not be created. Too many games are being hosted right now, join existing one", 7f);
        }
    }
}