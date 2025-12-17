using DNServerList;
using Mirror;
using Mirror.SimpleWeb;
using MultiFPS.ServerList;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MultiFPS
{
    public class UIServerListPlayButton : MonoBehaviour
    {
        // Start is called before the first frame update
        [SerializeField] Image _img;
        [SerializeField] Image _imgBar;
        [SerializeField] Image _imgBarBehind;
        [SerializeField] Color _bloomColor;
       // Color _defaultColor;

        [Header("QuickPlay")]
        [SerializeField] Button _serveGameButton;

        [SerializeField] UILoadingScreen _loadingScreen;
        private void Awake()
        {
         //   _defaultColor = _img.color;
         //   _imgBarBehind.color = _defaultColor;

            _serveGameButton.onClick.AddListener(OnClick);
        }

        private void OnEnable()
        {
            StartCoroutine(AnimateColor());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        void OnClick()
        {
            ServerListClient.Singleton.SendQuickPlayRequest();
            _loadingScreen.ShowLoadingScreen("Searching for match...", 15f);
        }

        public void OnQuickPlayFound(string address, ushort port)
        {

            _loadingScreen.ShowLoadingScreen("Match found, connecting...", 15f);
            StartCoroutine(Connect());
            IEnumerator Connect()
            {

                yield return new WaitForEndOfFrame();
                yield return new WaitForSeconds(0.2f);
                NetworkManager networkManager = NetworkManager.singleton;

                networkManager.networkAddress = address;
                networkManager.GetComponent<SimpleWebTransport>().port = port;
                networkManager.StartClient();
            }
        }
        public void OnLobbyCouldNotBeCreated()
        {
            _loadingScreen.HideLoadingScreen();
            _loadingScreen.ShowMessageScreen("Did not found any games", 4f);
        }

        IEnumerator AnimateColor()
        {
            float fill = 0;
            while (gameObject.activeInHierarchy)
            {
                fill += Time.deltaTime * 0.75f;

                _imgBar.fillAmount = fill;
                _imgBarBehind.fillAmount = fill - 0.1f;

                if (fill > 1.1f)
                    fill = 0;

                yield return null;
            }
        }
    }
}