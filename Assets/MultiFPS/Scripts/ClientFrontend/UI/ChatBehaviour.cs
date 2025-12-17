using UnityEngine;
using Mirror;
using System;
using MultiFPS.Gameplay;
using MultiFPS.UI;
using MultiFPS.Gameplay.Gamemodes;

namespace MultiFPS
{
    [RequireComponent(typeof(PlayerInstance))]
    public class ChatBehaviour : NetworkBehaviour
    {
        private PlayerInstance _playerInstance;
        public static ChatBehaviour _instance { get; private set; }

        //true if message is being typed by player
        public bool ChatWriting { private set; get; } = false;

        private void Awake()
        {
            _playerInstance = GetComponent<PlayerInstance>();
        }

        [Command(channel = 0)] //chat messages must go through reliable channel
        public void CmdRelayClientMessage(string message)
        {
            RpcHandleChatClientMessage(GameTools.CheckMessageLength(message));
        }

        [ClientRpc]
        public void RpcHandleChatClientMessage(string message)
        {
            if (!ClientInterfaceManager.Instance) return;

            Color colorForNickaname = _playerInstance.Team == -1 ? Color.white : ClientInterfaceManager.Instance.UIColorSet.TeamColors[_playerInstance.Team];

            //make final string from player nickname and his message
            string newMessage = $" {"<b>" + $"<color=#{ColorUtility.ToHtmlStringRGBA(colorForNickaname)}>" + _playerInstance.PlayerInfo.Username + "</b>" + "</color>" + ": " + message}";

            //write it to UI
            ChatUI.Instance.WriteMessageToChat(newMessage);
        }
    }
}