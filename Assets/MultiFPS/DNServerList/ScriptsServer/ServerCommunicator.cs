using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Threading;
using System.Collections.Generic;
using System;
#if !UNITY_WEBGL
using EasyComClient;
#endif
#if !UNITY_WEBGL
#endif

namespace DNServerList
{
    /// <summary>
    /// Class responsible for communication with DNServerList. 
    /// For example: When game state changes (player count changes), this class will notify server list
    /// so it can update it's data and display current state of servers in server list
    /// </summary>
    public class ServerCommunicator : MonoBehaviour
    {
        public static ServerCommunicator Singleton { get; private set; }

        //This event will be launched when server list app will boot this unity build. We should
        //to this event method that will further setup and run the game so it will be able to be connected to
        public UnityEvent<ushort, string> ServeGame;
        public UnityEvent<ushort> ServeQuickPlayLobby;
        

        [SerializeField] Text _text;

        public static string AccesCode;

#if !UNITY_WEBGL
        bool _terminateIfLobbyIsEmpty  = false;
        int _currentPlayerCount;

        public static int unityThread;
        static public Queue<Action> runInUpdate = new Queue<Action>();

        Coroutine _c_checkIgGameIsEmpty;
        EasyClientAPI _dnComCLientInterface;

        Response bootResponse;

        public void Awake()
        {
            unityThread = Thread.CurrentThread.ManagedThreadId;
        }

        void LogCommunicator(string log) 
        {
            RunOnUnityThread(() => Debug.Log(log));
        }

        private void Update()
        {
            while (runInUpdate.Count > 0)
            {
                Action action = null;
                lock (runInUpdate)
                {
                    if (runInUpdate.Count > 0)
                        action = runInUpdate.Dequeue();
                }
                action?.Invoke();
            }
        }

        protected async virtual void Start()
        {
            string[] args = Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; i++)
            {
                string[] command = args[i].Split('>');

                if (command[0] == "connect" && command.Length > 3)
                {
                    if (Singleton)
                    {
                        Destroy(this.gameObject);
                        return;
                    }

                    Singleton = this;
                    DontDestroyOnLoad(this.gameObject);

                    string address = command[1];
                    ushort port = Convert.ToUInt16(command[2]);
                    short internalAppID = Convert.ToInt16(command[3]);

                    _text.text += $"Connecting to {address}:{port} as {internalAppID}";

                    _dnComCLientInterface = new EasyClientAPI();
                    _dnComCLientInterface.OnLog = new Action<string>(LogCommunicator);

                    _dnComCLientInterface.RegisterEndpoint("serve", Cmd_ServeGame);
                    _dnComCLientInterface.RegisterEndpoint("kill", Cmd_KillGame);
                    _dnComCLientInterface.RegisterEndpoint("servequickplay", Cmd_ServeQuickPlay);
                    
                    _dnComCLientInterface.Callback_OnConnected += OnConnectedToMatchmakingSystem;
                    _dnComCLientInterface.Callback_OnDisconnected += OnDisconnectedFromMatchmakingSystemCause;
                    _dnComCLientInterface.Callback_CouldNotConnect += OnDisconnectedFromMatchmakingSystem;

                    _dnComCLientInterface.AssignSeatID(internalAppID);
                    //connect to the server list manager app
                    await _dnComCLientInterface.Connect(address, port);
                }

                if (command[0] == "terminatewhenempty" && command.Length > 1) 
                {
                    _terminateIfLobbyIsEmpty  = Convert.ToBoolean(command[1]);

                    if (_terminateIfLobbyIsEmpty)
                        _c_checkIgGameIsEmpty = StartCoroutine(Server_CheckIfGameIsEmpty());
                }

                if (command[0] == "setAccessCode" && command.Length > 1) 
                {
                    AccesCode = command[1];
                }
            }
        }

        void OnConnectedToMatchmakingSystem() 
        {
            RunOnUnityThread(()=>_text.text += "Connected to master");
        }

        void OnDisconnectedFromMatchmakingSystemCause(DisconnectCause cause) => OnDisconnectedFromMatchmakingSystem();

        void OnDisconnectedFromMatchmakingSystem()
        {
            RunOnUnityThread(() => Application.Quit());
        }

        ///This should be launched every single time someone joins or disconnects. Otherwise DNServerList may recognize
        ///game incorrectly as being empty and shutdown it
        public virtual void OnPlayerCountChanged(int currentPlayerCount, string lobbyProperties)
        {
            _currentPlayerCount = currentPlayerCount;

            if (_c_checkIgGameIsEmpty != null)
                StopCoroutine(_c_checkIgGameIsEmpty);

            if (currentPlayerCount <= 0 && _terminateIfLobbyIsEmpty)
                _dnComCLientInterface.Disconnect();

            _dnComCLientInterface.SendRequest("updatelobbymetadata", lobbyProperties);
        }


        //if game running on server is empty (no players in game) then terminate it
        IEnumerator Server_CheckIfGameIsEmpty()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(20f);

                //close game if player count is 0
                if(_currentPlayerCount == 0)
                    Application.Quit(0);
            }
        }

        /// <summary>
        /// Kill game on DNServerList command
        /// </summary>
        protected void Cmd_KillGame(string argsm, Response res)
        {
            RunOnUnityThread(() => {
                _text.text += $"{argsm}\n";
                Application.Quit(0);
                res.Respond(0, string.Empty);
            });
        }

        /// <summary>
        /// Server QuickPlay on 
        /// </summary>
        private void Cmd_ServeQuickPlay(string argsm, Response res)
        {
            RunOnUnityThread(() => {
                _text.text += $"COMMANDED TO SERVE {argsm}";
                
                bootResponse = res;
                ServeQuickPlayLobby?.Invoke(Convert.ToUInt16(argsm));
            });
        }


        /// <summary>
        /// Boot server on DNServerList command
        /// </summary>
        void Cmd_ServeGame(string argsm, Response res) 
        {
            RunOnUnityThread(() =>
            {
                Post_BootUserLobby cmd = JsonUtility.FromJson<Post_BootUserLobby>(argsm);

                _text.text += $"COMMANDED TO SERVE {argsm}";

                bootResponse = res;
                ServeGame?.Invoke(Convert.ToUInt16(cmd.Port), cmd.GameSettings);
            });
        }

        //Run this method when game is fully booted, so DNServer list will list this game and others will be able to
        //see it in list and join it
        public void OnGameReady(string data)
        {
            _text.text += $"SENDED READY INFO {data}";

            bootResponse.Respond(0, data);
        }

        public static void RunOnUnityThread(Action action)
        {
            if (unityThread == Thread.CurrentThread.ManagedThreadId)
            {
                action();
            }
            else
            {
                lock (runInUpdate)
                {
                    runInUpdate.Enqueue(action);
                }
            }
        }
        public struct Post_BootUserLobby
        {
            public ushort Port;
            public string GameSettings;
        }
#endif
    }
}