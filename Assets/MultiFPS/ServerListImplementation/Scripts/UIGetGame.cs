using DNServerList;
using Mirror;
using Mirror.SimpleWeb;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MultiFPS;
using MultiFPS.ServerList;

namespace MultiFPS.UI{
public class UIGetGame : MonoBehaviour
{
    [SerializeField] UILoadingScreen _loadingScreen;

    [SerializeField] InputField _ifAccess;
    [SerializeField] Button _btnConnect;

    [SerializeField] Text _feedback;

    private void Awake()
    {
        _feedback.text = string.Empty;
    }

    public void SendRequest()
    {
        string code = _ifAccess.text;

        if (code.Length != 5)
        {
            _feedback.text = "Enter valid code";
            return;
        }

        ServerListClient.Singleton.GetServerByCode(code);
    }

    public void OnLobbyFound(string address, ushort port)
    {
        _loadingScreen.ShowLoadingScreen("Game found!\nConnecting...", 5f);
        StartCoroutine(Connect());
        IEnumerator Connect()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(0.2f);
            DNNetworkManager networkManager = DNNetworkManager.Instance;

            networkManager.networkAddress = address;
            networkManager.Action_SetNetworkManagerPort?.Invoke(port);
            networkManager.StartClient();
        }
    }
    public void OnLobbyNotFound()
    {
        _loadingScreen.ShowMessageScreen("Room not found", 2f);
    }

}
}