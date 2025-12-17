using Mirror;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DNUploader.Examples
{
    public class ClientCommand : NetworkBehaviour
    {
        [SyncVar(hook = "Client_OnCmdsReceived")]
        string cmds;

        [SerializeField] Text _text;
        private void Awake()
        {
            _text.text = string.Empty;
        }

        void Start()
        {
            string[] commands = Environment.GetCommandLineArgs();

            var allCommands = new StringBuilder();
            for (int i = 1; i < commands.Length; i++)
            {
                allCommands.Append(commands[i]);
                allCommands.Append(" ");
            }

            cmds = allCommands.ToString();
        }

        void Client_OnCmdsReceived(string oldValue, string newValue)
        {
            _text.text = cmds;
        }
    }
}