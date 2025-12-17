//using UnityEditor;
//using UnityEngine;
//using UnityEngine.UI;
//using System.Collections.Generic;
//using System.Linq;
//using System;
//using System.Diagnostics;
//using DN_Uploader;
//using System.Threading;


//namespace DNUploader_Editor
//{
//    public class DNUploader : EditorWindow
//    {
//        public LogBox ClientLog;
//        public LogBox ServerLog;
//        public LogBox PlayerLog;


//        #region player prefs keys
//        readonly static string _pp_connect_address = "DNUploader_connect_address";
//        readonly static string _pp_connect_port = "DNUploader_connect_port";
//        readonly static string _pp_playerName = "DNUploader_playerName";
//        readonly static string _pp_playerDestination = "DNUploader_playerDestination";
//        readonly static string _pp_target = "DNUploader_target";
//        readonly static string _pp_launchCommands = "DNUploader_launchCommands";
//        readonly static string _pp_developmentBuild = "DNUploader_developmentBuild";
//        #endregion

//        ConnectionPanel _connectionPanel;
//        ConnectedPanel _connectedPanel;
//        ServerBuildPanel _serverBuildPanel;
//        ServerBuildLauncher _serverBuildLauncher;

//        public UnityEngine.UIElements.ScrollView RootVerticalScrollElement;

//        #region user preferences
//        string userPath;
//        #endregion

//        #region session
//        bool _isConnected = false;
//        #endregion

//        public static int MaxLinesInLogs = 90;

//        public Button ConnectButton;

//        public static int unityThread;
//        static public Queue<Action> runInUpdate = new Queue<Action>();

//        public void Awake()
//        {
//            unityThread = Thread.CurrentThread.ManagedThreadId;
//        }

//        private void Update()
//        {
//            while (runInUpdate.Count > 0)
//            {
//                Action action = null;
//                lock (runInUpdate)
//                {
//                    if (runInUpdate.Count > 0)
//                        action = runInUpdate.Dequeue();
//                }
//                action?.Invoke();
//            }
//        }

//        public static void RunOnUnityThread(Action action)
//        {
//            if (unityThread == Thread.CurrentThread.ManagedThreadId)
//            {
//                action();
//            }
//            else
//            {
//                lock (runInUpdate)
//                {
//                    runInUpdate.Enqueue(action);
//                }
//            }
//        }


//        [MenuItem("DNTools/DNUploader")]
//        public static void ShowExample()
//        {
//            DNUploader wnd = GetWindow<DNUploader>();
//            wnd.titleContent = new GUIContent("DN_Uploader");

//            wnd.minSize = new Vector2(450, 600);
//            wnd.maxSize = new Vector2(600, 1440);

//            Log("Opened DN_Uploader");
//        }

//        public void CreateGUI()
//        {
//            DNUploaderInterface.Init();

//            DNUploaderInterface.DNUploader_Callback_OnConnected += DNU_OnConnected;
//            DNUploaderInterface.DNUploader_Callback_OnDisconnected += DNU_OnDisconnect;

//            DNUploaderInterface.DNUploader_Callback_ProgressBar += DNU_ProgressBar;

//            DNUploaderInterface.DNUploader_Callback_ReceivedServerLog += DNU_ReceivedServerLog;
//            DNUploaderInterface.DNUploader_Callback_ReceivedPlayerLog += DNU_ReceivedPlayerLog;
//            DNUploaderInterface.DNUploader_Callback_WriteClientLog += DNU_WriteClientLog;


//            RootVerticalScrollElement = new UnityEngine.UIElements.ScrollView(UnityEngine.UIElements.ScrollViewMode.Vertical);

//            //initialize all panels when window opens, then we will show and hide them as needed
//            _connectionPanel = new ConnectionPanel(this);
//            _connectedPanel = new ConnectedPanel(this);
//            _serverBuildPanel = new ServerBuildPanel(this);
//            _serverBuildLauncher = new ServerBuildLauncher(this);

//            ClientLog = new LogBox("Editor log");
//            ServerLog = new LogBox("Server log");
//            PlayerLog = new LogBox("Player log");

//            ShowConnectionMenu();

//            Log("Panels initialized, events subscribed");

//            if (_isConnected)
//                _connectionPanel.Connect();

//        }

//        #region progress bar

//        void DNU_ProgressBar(string info, float progress)
//        {
//            RunOnUnityThread(Action);

//            void Action()
//            {
//                Log($"{info}, progress {progress}");

//                if (progress < 0)
//                {
//                    EditorUtility.ClearProgressBar();
//                    return;
//                }

//                EditorUtility.DisplayProgressBar("DN_Uploader", info, progress);
//            }
//        }

//        void DNU_ReceivedServerLog(string serverLog)
//        {
//            RunOnUnityThread(Action);

//            void Action() { ServerLog.AddLogLine(serverLog); }
//        }

//        void DNU_ReceivedPlayerLog(string playerLog)
//        {
//            RunOnUnityThread(Action);

//            void Action() { PlayerLog.AddLogLine(playerLog); }
//        }

//        void DNU_WriteClientLog(string clientLog)
//        {
//            RunOnUnityThread(Action);

//            void Action() { ClientLog.AddLogLine(clientLog); }
//        }

//        #endregion

//        public void ShowConnectionMenu()
//        {
//            ResetWindow();
//            DNU_ProgressBar("DN", -1f); //hide progress bar in case it was shown during panel reset

//            wantsMouseMove = true;
//            wantsLessLayoutEvents = false;

//            //insert connection panel
//            RootVerticalScrollElement.Insert(0, _connectionPanel.panel);
//            RootVerticalScrollElement.Insert(1, ClientLog.panel);
//            RootVerticalScrollElement.Insert(2, ServerLog.panel);
//            RootVerticalScrollElement.Insert(3, PlayerLog.panel);
//            _connectionPanel.SetConnectButton(false);
//            Log("Window reset to connect panel");
//        }

//        public void ResetWindow()
//        {
//            rootVisualElement.Clear();
//            RootVerticalScrollElement.Clear();

//            var spriteImage = new Image();
//            // spriteImage.scaleMode = ScaleMode.ScaleToFit;
//            //spriteImage.scaleMode = ScaleMode.StretchToFill;
//            spriteImage.style.alignItems = Align.Stretch;
//            spriteImage.style.display = DisplayStyle.Flex;
//            spriteImage.style.justifyContent = Justify.SpaceAround;
//            spriteImage.style.overflow = Overflow.Visible;
//            spriteImage.style.position = Position.Relative;
//            spriteImage.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;
//            spriteImage.style.backgroundColor = Color.black;
//            spriteImage.sprite = Resources.Load<Sprite>("sprite_dnUploader_cover");


//            Button cover = new Button(()=> Process.Start("http://desnetware.net"));

//            cover.tooltip = "Visit our website desnetware.net for more informations & tools";

//            cover.style.backgroundColor = Color.black; //give more or less matching background color for nonstandard stretching
//            cover.style.position = Position.Relative;
//            //cover.style.alignSelf = Align.FlexStart; //allign cover to the left

//            cover.style.justifyContent = Justify.SpaceAround;
//            cover.style.alignItems = Align.Center;
//            cover.style.marginBottom = 0;
//            cover.style.marginTop = 0;
//            cover.style.marginLeft = 0;
//            cover.style.marginRight = 0;
//            cover.style.paddingBottom = 0;
//            cover.style.paddingTop = 0;
//            cover.style.paddingLeft = 0;
//            cover.style.paddingRight = 0;

//            cover.Add(spriteImage);

//            rootVisualElement.Insert(0, cover);
//            rootVisualElement.Insert(1, RootVerticalScrollElement);
//        }

//        public void PositionLogs()
//        {
//            ClientLog.panel.BringToFront();
//            ServerLog.panel.BringToFront();
//            PlayerLog.panel.BringToFront();
//        }

//        #region Panels

//        public class LogBox
//        {
//            public Box panel;

//            Label _logLabel;

//            public void Clear()
//            {
//                _logLabel.text = string.Empty;
//            }
//            public void AddLogLine(string line)
//            {
//                string logLine = string.IsNullOrEmpty(_logLabel.text) ? string.Empty : "\n";
//                logLine += $"{DateTime.Now} - {line}";

//                string content = _logLabel.text += logLine;
//                int extraLines = GetLineCount(content) - MaxLinesInLogs;
//                if (extraLines > 0)
//                {
//                    content = DeleteLines(content, extraLines);
//                }
//                _logLabel.text = content;
//            }
//            public LogBox(string logName)
//            {
//                Box box = DNUBox();

//                Foldout foldout = new Foldout();
//                foldout.text = logName;

//                _logLabel = new Label(string.Empty);
//                _logLabel.enableRichText = true;

//                ScrollView scroll = new ScrollView(ScrollViewMode.Vertical);

//                foldout.Add(scroll);

//                scroll.Add(_logLabel);

//                panel = box;
//                panel.Add(foldout);
//            }

//            public static string DeleteLines(string s, int linesToRemove)
//            {
//                return s.Split(Environment.NewLine.ToCharArray(),
//                               linesToRemove + 1
//                    ).Skip(linesToRemove)
//                    .FirstOrDefault();
//            }
//            public static int GetLineCount(string input)
//            {
//                int lineCount = 0;

//                for (int i = 0; i < input.Length; i++)
//                {
//                    switch (input[i])
//                    {
//                        case '\r':
//                            {
//                                if (i + 1 < input.Length)
//                                {
//                                    i++;
//                                    if (input[i] == '\r')
//                                    {
//                                        lineCount += 2;
//                                    }
//                                    else
//                                    {
//                                        lineCount++;
//                                    }
//                                }
//                                else
//                                {

//                                    lineCount++;
//                                }
//                            }
//                            break;
//                        case '\n':
//                            lineCount++;
//                            break;
//                        default:
//                            break;
//                    }
//                }
//                return lineCount;
//            }
//        }

//        public class ConnectionPanel
//        {
//            DNUploader _root;

//            public Box panel;

//            TextField address;
//            IntegerField port;

//            bool _connecting = true;

//            public short Port() { return (short)port.value; }
//            public string Address() { return address.value; }

//            public ConnectionPanel(DNUploader root)
//            {
//                _root = root;
//                panel = Panel();

//            }

//            Box Panel()
//            {
//                Box connectionBox = new Box();

//                connectionBox.style.position = Position.Relative;
//                connectionBox.style.marginRight = 30;
//                connectionBox.style.marginLeft = 30;
//                connectionBox.style.top = 30;
//                connectionBox.style.marginBottom = 30;

//                Label connectLabel = new Label("Connect");

//                address = new TextField("Address: ", 20, false, false, '*');

//                string pp_address = PlayerPrefs.GetString(_pp_connect_address);

//                address.value = !String.IsNullOrEmpty(pp_address) ? pp_address : "localhost";
//                address.RegisterValueChangedCallback(OnAddressChanged);

//                port = new IntegerField("Port: ");

//                int pp_port = PlayerPrefs.GetInt(_pp_connect_port);

//                port.value = pp_port != 0 ? pp_port : 7777;
//                port.RegisterValueChangedCallback(OnPortChanged);

//                //setup connect button
//                _root.ConnectButton = new Button(Connect);
//                SetConnectButton(false);

//                connectionBox.Insert(0, connectLabel);
//                connectionBox.Insert(1, address);
//                connectionBox.Insert(2, port);
//                connectionBox.Insert(3, _root.ConnectButton);

//                return connectionBox;
//            }

//            public void Connect()
//            {
//                if (_connecting) return;

//                Log($"Connecting to: {Address()}:{Port()}");

//                SetConnectButton(true);

//                DNUploaderInterface.Connect(Address(), (ushort)System.Convert.ToInt32(Port()));
//            }

//            public void SetConnectButton(bool connecting)
//            {
//                if (_connecting == connecting) return; //dont set same state twice
//                _connecting = connecting;

//                _root.ConnectButton.style.position = Position.Relative;

//                _root.ConnectButton.style.backgroundColor = connecting ? Color.gray : Color.green;
//                _root.ConnectButton.style.color = connecting ? Color.white : Color.black;
//                _root.ConnectButton.SetEnabled(!connecting);

//                _root.ConnectButton.text = connecting ? "Connecting..." : "Connect";

//                _root.ConnectButton.style.unityFontStyleAndWeight = FontStyle.Bold;
//            }

//            void OnAddressChanged(ChangeEvent<string> evt)
//            {
//                PlayerPrefs.SetString(_pp_connect_address, address.value);
//            }
//            void OnPortChanged(ChangeEvent<int> evt)
//            {
//                PlayerPrefs.SetInt(_pp_connect_port, port.value);
//            }
//        }

//        public class ConnectedPanel
//        {
//            Label _connectionStateLog;

//            public Box panel;
//            public ConnectedPanel(DNUploader root)
//            {
//                panel = Panel();
//            }

//            void Disconnect()
//            {
//                DNUploaderInterface.Disconnect();
//            }

//            public void WriteConnectionLog(string log)
//            {
//                _connectionStateLog.text = log;
//            }

//            Box Panel()
//            {
//                Box connectedBox = new Box();

//                _connectionStateLog = new Label("Connected to:");
//                _connectionStateLog.style.color = Color.green;
//                _connectionStateLog.style.fontSize = 15;

//                connectedBox.style.position = Position.Relative;
//                connectedBox.style.marginRight = 30;
//                connectedBox.style.marginLeft = 30;
//                connectedBox.style.marginTop = 30;
//                connectedBox.style.marginBottom = 30;

//                //setup disconect button
//                Button disconnectButton = new Button(Disconnect);
//                disconnectButton.text = "Disconnect";
//                disconnectButton.style.position = Position.Relative;
//                disconnectButton.style.color = Color.yellow;

//                connectedBox.Add(_connectionStateLog);
//                connectedBox.Add(disconnectButton);
//                return connectedBox;
//            }
//        }

//        public class ServerBuildPanel
//        {
//            DNUploader _root;

//            public Box panel;

//            TextField _buildName;
//            DropdownField _targetDropDown;

//            Toggle _developmentBuild;

//            Button _buildAndUploadButton;

//            List<string> _targetOptions = new List<string>();
//            Target _selectedtarget;

//            public ServerBuildPanel(DNUploader root)
//            {
//                _root = root;
//                panel = Panel();

//                _selectedtarget = (Target)PlayerPrefs.GetInt(_pp_target);
//                _targetDropDown.value = _targetOptions[(int)_selectedtarget];
//            }

//            private void OnTargetSelected(ChangeEvent<string> evt)
//            {
//                for (int i = 0; i < _targetOptions.Count; i++)
//                {
//                    if (_targetOptions[i] == _targetDropDown.value)
//                    {
//                        _selectedtarget = (Target)i;
//                        PlayerPrefs.SetInt(_pp_target, i);
//                        return;
//                    }
//                }
//            }

//            public void SetTarget(int target) 
//            {
//                _targetDropDown.SetValueWithoutNotify(_targetOptions[target]);
//                _targetDropDown.SetEnabled(false);

//                _selectedtarget = (Target)target;
//            }

//            private void OnPlayerBuildNameChanged(ChangeEvent<string> evt)
//            {
//                PlayerPrefs.SetString(_pp_playerName, _buildName.value);
//            }
//            void OpenBuildFolder()
//            {
//                _root.userPath = PlayerPrefs.GetString(_pp_playerDestination);

//                if (string.IsNullOrEmpty(_root.userPath))
//                {
//                    LogWarning("There is no build destination selected");
//                    return;
//                }

//                Process.Start(_root.userPath);
//            }

//            void ChangeBuildFolder()
//            {
//                _root.userPath = _root.GetBuildPlayerLocation();
//                PlayerPrefs.SetString(_pp_playerDestination, _root.userPath);
//            }

//            void BuildServerBuild()
//            {
//                if (string.IsNullOrEmpty(_buildName.value)) _buildName.value = "game";

//                _root._serverBuildLauncher.DNU_OnReceivedPlayerState("", 3, (int)_selectedtarget); //change ui state

//                //replace illegal characters if they were used in build name
//                string illegalCharacters = $"\"/:*?<>|" + (char)92;

//                for (int i = 0; i < illegalCharacters.Length; i++)
//                {
//                    if (!_buildName.value.Contains(illegalCharacters[i])) continue;

//                    _buildName.value = _buildName.value.Replace(illegalCharacters[i], 'X');
//                }

//                _root.ClientLog.AddLogLine("Building server player");

//                SetBuildAndUploadButton(false, "Uploading...");

//                string path;
//                _root.userPath = PlayerPrefs.GetString(_pp_playerDestination);

//                if (string.IsNullOrEmpty(_root.userPath))
//                {
//                    path = _root.GetBuildPlayerLocation();
//                    PlayerPrefs.SetString(_pp_playerDestination, path);
//                }
//                else
//                    path = _root.userPath;

//                string[] levels;

//                List<string> includedLevels = new List<string>();

//                for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
//                {
//                    if (EditorBuildSettings.scenes[i].enabled)
//                        includedLevels.Add(EditorBuildSettings.scenes[i].path);
//                }

//                levels = includedLevels.ToArray();

//                string buildPath = path;

//                string executableName = _buildName.value;

//                if (_selectedtarget == Target.Linux)
//                    executableName += ".x86_64";
//                else
//                    executableName += ".exe";

//                buildPath += $"/{executableName}";

//                BuildPlayerOptions buildOptions = new BuildPlayerOptions
//                {
//                    locationPathName = buildPath,
//                    target = _selectedtarget == Target.Linux ? BuildTarget.StandaloneLinux64 : BuildTarget.StandaloneWindows,
//                    subtarget = (int)StandaloneBuildSubtarget.Server,
//                    scenes = levels,
//                };

//                if (_developmentBuild.value)
//                    buildOptions.options = BuildOptions.Development;

//                BuildPipeline.BuildPlayer(buildOptions);
//                DNUploaderInterface.SendPlayerToServer(path, executableName, _buildName.value);  
//            }

//            public void SetBuildAndUploadButton(bool enabled, string label = "") 
//            {
//                _buildAndUploadButton.text = enabled? "Build and upload": label;
//                _buildAndUploadButton.SetEnabled(enabled);
//            }

//            void OnDevelopmentBuildChecked(ChangeEvent<bool> evt)
//            {
//                PlayerPrefs.SetInt(_pp_developmentBuild, System.Convert.ToInt32(_developmentBuild.value));
//            }

//            public Box Panel()
//            {
//                //set build name input field
//                _buildName = new TextField("Server build name");
//                _buildName.RegisterValueChangedCallback(OnPlayerBuildNameChanged);
//                string savedPlayerName = PlayerPrefs.GetString(_pp_playerName);
//                _buildName.value = string.IsNullOrEmpty(savedPlayerName) ? "game" : savedPlayerName;

//                //set target dropdown
//                _targetOptions = Enum.GetNames(typeof(Target)).ToList();
//                _targetDropDown = new DropdownField("Target", _targetOptions, 0);
//                _targetDropDown.RegisterValueChangedCallback(OnTargetSelected);
//                _targetDropDown.SetEnabled(false);

//                //set build button
//                _buildAndUploadButton = new Button(BuildServerBuild);
//                _buildAndUploadButton.style.height = 30;
//                _buildAndUploadButton.style.marginBottom = 15;
//                _buildAndUploadButton.style.marginTop = 5;
//                SetBuildAndUploadButton(true);

//                //set open folder button
//                Button openBuildFolderBtn = new Button(OpenBuildFolder);
//                openBuildFolderBtn.text = "Open build player location";
//                //openBuildFolderBtn.style.marginBottom = 5;

//                //sett new build location button
//                Button newBuildFolderLocation = new Button(ChangeBuildFolder);
//                newBuildFolderLocation.text = "Change build player location";

//                Box serverPanel = DNUBox("Build Settings");

//                _developmentBuild = new Toggle("DevelopmentBuild");
//                _developmentBuild.value = PlayerPrefs.GetInt(_pp_developmentBuild) > 0 ? true: false;
//                _developmentBuild.RegisterValueChangedCallback(OnDevelopmentBuildChecked);


//                Button _showBuildPlayerWindow = new Button(BuildPlayerWindow.ShowBuildPlayerWindow);
//                _showBuildPlayerWindow.text = "Show build player window";
//                //_showBuildPlayerWindow.style.marginTop = 5;

//                serverPanel.Insert(1, _buildName);
//                serverPanel.Insert(2, _developmentBuild);
//                serverPanel.Insert(3, _targetDropDown);
//                serverPanel.Insert(4, _buildAndUploadButton);
//                serverPanel.Insert(5, openBuildFolderBtn);
//                serverPanel.Insert(6, newBuildFolderLocation);
//                serverPanel.Insert(7, newBuildFolderLocation);
//                serverPanel.Add(_showBuildPlayerWindow);

//                return serverPanel;
//            }
//        }

//        public class ServerBuildLauncher
//        {
//            DNUploader _root;

//            public Box panel;

//            TextField _launchCommands;
//            Button _runBuild;
//            Button _stopPlayer;
//            Label _buildNameLabel;

//            string buildName;

//            public ServerBuildLauncher(DNUploader root)
//            {
//                _root = root;
//                panel = BuildPresentOnServerPanel();

//                DNUploaderInterface.DNUploader_Callback_ReceivedAvailablePlayerInfo += DNU_OnReceivedPlayerState;

//                WriteServerBuildName();
//            }

//            Box BuildPresentOnServerPanel()
//            {
//                Box box = DNUBox("Server build launcher");

//                _buildNameLabel = new Label("label");
//                _buildNameLabel.style.fontSize = 15;

//                _launchCommands = new TextField("Launch commands: ", 500, false, false, '*');
//                _launchCommands.RegisterValueChangedCallback(OnLaunchCommandsChanged);
//                _launchCommands.value = "";

//                _runBuild = new Button(RunServerPlayer);

//                _runBuild.text = "Run server player";
//                _runBuild.SetEnabled(false);

//                _stopPlayer = new Button(StopServerPlayer);

//                box.Insert(1, _buildNameLabel);
//                box.Insert(2, _launchCommands);
//                box.Insert(3, _runBuild);

//                _launchCommands.value = PlayerPrefs.GetString(_pp_launchCommands);

//                return box;
//            }

//            void RunServerPlayer()
//            {
//                _root.PlayerLog.Clear();

//                DNUploaderInterface.RunServerPlayer(_launchCommands.value);
//                _runBuild.SetEnabled(false);
//                _runBuild.text = "Running server player..";
//            }
//            void StopServerPlayer()
//            {
//                DNUploaderInterface.StopServerPlayer();
//                _stopPlayer.SetEnabled(false);
//                _stopPlayer.text = "Stopping server player...";
//            }

//            public void DNU_OnReceivedPlayerState(string name, int playerState, int playerTarget)
//            {
//                RunOnUnityThread(Action);
//                void Action()
//                {
                    

//                    buildName = name;

//                    //disable build button if server player is already running on server
//                    _root._serverBuildPanel.SetBuildAndUploadButton(playerState != 2 && playerState != 3, playerState == 2 ? "Server player running" : playerState == 3? "Uploading...": null);
//                    _root._serverBuildPanel.SetTarget(playerTarget);

//                    WriteServerBuildName(playerState);

//                    if (playerState == 0)
//                    {
//                        panel.RemoveAt(3);
//                        panel.Insert(3, _runBuild);

//                        _runBuild.text = "Run server player";

//                        _runBuild.SetEnabled(false);
//                    }
//                    else if (playerState == 1) //player uploaded but not running, setup button for launching process on server
//                    {
//                        panel.RemoveAt(3);
//                        panel.Insert(3, _runBuild);

//                        _runBuild.text = "Run server player";
//                        _runBuild.style.backgroundColor = Color.green;
//                        _runBuild.style.color = Color.black;

//                        _runBuild.SetEnabled(true);
//                    }
//                    else if (playerState == 2) //player uploaded and running, setup button for killing process on server
//                    {
//                        panel.RemoveAt(3);
//                        panel.Insert(3, _stopPlayer);

//                        _stopPlayer.text = "Stop server player";
//                        _stopPlayer.style.backgroundColor = Color.red;
//                        _stopPlayer.style.color = Color.white;

//                        _stopPlayer.SetEnabled(true);
//                    }
//                    else if (playerState == 3) //server build is being uploaded
//                    {
//                        _runBuild.SetEnabled(false);
//                        //_runBuild.text = "Server build is being uploaded...";
//                        _launchCommands.SetEnabled(false);

//                        _buildNameLabel.style.color = Color.cyan;
//                        _buildNameLabel.text = "Server build is being uploaded...";
//                    }
//                }
//            }

//            public void WriteServerBuildName(int code = 0)
//            {
//                if (code == 0)
//                {
//                    _launchCommands.SetEnabled(false);
//                    _buildNameLabel.style.color = Color.yellow;
//                    _buildNameLabel.text = "Server build is not uploaded yet";
//                }
//                else
//                {
//                    _launchCommands.SetEnabled(true);
//                    _buildNameLabel.style.color = Color.green;
//                    _buildNameLabel.text = $"Uploaded build: {buildName}";
//                }
//            }


//            private void OnLaunchCommandsChanged(ChangeEvent<string> evt)
//            {
//                PlayerPrefs.SetString(_pp_launchCommands, _launchCommands.value);
//            }
//        }
//        #endregion

//        #region STYLES
//        public static Box DNUBox(string labelName = "")
//        {
//            Box box = new Box();

//            box.style.position = Position.Relative;
//            box.style.marginRight = 30;
//            box.style.marginLeft = 30;
//            box.style.marginTop = 15;
//            box.style.marginBottom = 15;

//            if (string.IsNullOrEmpty(labelName))
//                return box;

//            Label label = new Label(labelName);
//            label.style.fontSize = 15;
//            box.Add(label);

//            return box;
//        }
//        #endregion


//        #region DNUploaderInterfaceCallbacks
//        public void DNU_OnDisconnect(int code)
//        {
//            RunOnUnityThread(OnDisconnect);

//            void OnDisconnect()
//            {
//                _connectionPanel.SetConnectButton(false);

//                ShowConnectionMenu();

//                _isConnected = false;

//                switch (code)
//                {
//                    case -1:
//                        ClientLog.AddLogLine("Could not connect to the server");
//                        break;
//                    case 0:
//                        ClientLog.AddLogLine("Disconnected from server");
//                        break;
//                    case 1:
//                        ClientLog.AddLogLine("Lost connection with server");
//                        break;
//                }
//            }
//        }



//        void DNU_OnConnected()
//        {
//            Log("Connected to the server");

//            _connectionPanel.SetConnectButton(false);

//            RootVerticalScrollElement.RemoveAt(0); //remove connection panel where we pass adress and port

//            ClientLog.AddLogLine("Connected to the server");

//            RootVerticalScrollElement.Add(_connectedPanel.panel);
//            RootVerticalScrollElement.Add(_serverBuildPanel.panel);
//            RootVerticalScrollElement.Add(_serverBuildLauncher.panel);

//            _connectedPanel.WriteConnectionLog($"Connected to: {_connectionPanel.Address()}:{_connectionPanel.Port()}");

//            PositionLogs();

//            _isConnected = true;
//        }
//        #endregion

//        string GetBuildPlayerLocation()
//        {
//            return EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");
//        }

//        #region Logger
//        static DebugLogLevels _currentLogLevel = DebugLogLevels.None;
//        public enum DebugLogLevels
//        {
//            None,
//            Log,
//            Warning,
//            Error
//        }

//        public static void Log(string msg)
//        {
//            if (_currentLogLevel >= DebugLogLevels.Log)
//                UnityEngine.Debug.Log($"DNUploader: {msg}");

//        }
//        public static void LogWarning(string msg)
//        {
//            if (_currentLogLevel >= DebugLogLevels.Warning)
//                UnityEngine.Debug.LogWarning($"DNUploader: {msg}");
//        }
//        public static void LogError(string msg)
//        {
//            if (_currentLogLevel >= DebugLogLevels.Error)
//                UnityEngine.Debug.LogError($"DNUploader: {msg}");
//        }
//        #endregion

//        private void OnDestroy()
//        {
//            DNUploaderInterface.Disconnect();

//            DNUploaderInterface.DNUploader_Callback_OnConnected -= DNU_OnConnected;

//            DNUploaderInterface.DNUploader_Callback_OnDisconnected -= DNU_OnDisconnect;

//            Log("Window closed");
//        }
//        enum Target
//        {
//            Windows,
//            Linux
//        }
//    }
//}