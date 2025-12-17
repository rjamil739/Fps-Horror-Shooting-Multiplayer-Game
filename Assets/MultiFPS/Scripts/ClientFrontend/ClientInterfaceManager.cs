using UnityEngine.SceneManagement;
using UnityEngine;
using MultiFPS.Gameplay;
using MultiFPS.UI.HUD;
using System.Collections.Generic;
using Mirror;

using MultiFPS.Gameplay.Gamemodes;
using MultiFPS.UI.Gamemodes;

namespace MultiFPS.UI {
    [DisallowMultipleComponent]
    public class ClientInterfaceManager : MonoBehaviour
    {
        public static ClientInterfaceManager Instance;

        //UI prefabs
        public GameObject PauseMenuUI;
        public GameObject ChatUI;
        public GameObject ScoreboardUI;
        public GameObject KillfeedUI;
        public GameObject PlayerHudUI;
        public GameObject GameplayCamera;
        [SerializeField] GameObject[] _additionalUI;

        //these colors are here because we may want to adjust them easily in the inspector
        public UIColorSet UIColorSet;

        public SkinContainer[] characterSkins;
        public ItemSkinContainer[] ItemSkinContainers;

        [Header("Gamemodes UI Prefabs")]
        [SerializeField] GameObject[] gamemodesUI;

        public void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                //if this happens it means that player returns to hub scene with Client Manager from previous hub scene load, so we dont
                //need another one, so destroy this one

                //Destroy(gameObject);
                return;
            }


            SceneManager.sceneLoaded += OnSceneLoaded;

            UserSettings.SelectedItemSkins = new int[ItemSkinContainers.Length];

            for (int i = 0; i < ItemSkinContainers.Length; i++)
            {
                UserSettings.SelectedItemSkins[i] = -1;
            }

            ClientFrontend.ClientEvent_OnJoinedToGame += InstantiateUIforGivenGamemode;

            //by default cursor is hidden, show it for main menu
            ClientFrontend.ShowCursor(true);
        }

        /// <summary>
        /// This method will instantiate UI proper for gamemode, for example if we play Defuse gamemode, spawn UI wchich have
        /// score numbers for both teams, and bomb icon which we will display and color in red when bomb is planted
        /// </summary>
        void InstantiateUIforGivenGamemode(Gamemode gamemode, NetworkIdentity player)
        {
            int gamemodeID = (int)gamemode.Indicator;

            if (gamemodeID >= gamemodesUI.Length || gamemodesUI[gamemodeID] == null) return; //no ui for this gamemode avaible

            Instantiate(gamemodesUI[gamemodeID]).GetComponent<UIGamemode>().SetupUI(gamemode, player);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            var index = SceneManager.GetActiveScene().buildIndex;

            ClientFrontend.Hub = (index == 0);

            ClientFrontend.ShowCursor(index == 0);
            ClientFrontend.SetClientTeam(-1);

            //if we loaded non-hub scene, then spawn all the UI prefabs for player, then on disconnecting they will
            //be destroyed by scene unloading
            if (index != 0)
            {
                if (PauseMenuUI)
                    Instantiate(PauseMenuUI);
                if (ChatUI)
                    Instantiate(ChatUI);
                if (ScoreboardUI)
                    Instantiate(ScoreboardUI);
                if (KillfeedUI)
                    Instantiate(KillfeedUI);
                if (PlayerHudUI)
                    Instantiate(PlayerHudUI).GetComponent<Crosshair>().Setup();

                if(_additionalUI != null)
                    for (int i = 0; i < _additionalUI.Length; i++)
                    {
                        Instantiate(_additionalUI[i]);
                    }
            }
        }

        private void OnDestroy()
        {
            ClientFrontend.ClientEvent_OnJoinedToGame -= InstantiateUIforGivenGamemode;
        }
    }

    [System.Serializable]
    public class ItemSkinContainer
    {
        public string ItemName;
        public SingleItemSkinContainer[] Skins;
    }
    [System.Serializable]
    public class SingleItemSkinContainer
    {
        public string SkinName;
        public Material Skin;
    }
}
