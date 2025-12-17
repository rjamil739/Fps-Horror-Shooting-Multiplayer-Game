using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MultiFPS.Gameplay.Gamemodes;
using MultiFPS.Gameplay;
using MultiFPS.UI.Gamemodes;

namespace MultiFPS.UI
{
    public static class ClientFrontend
    {
        //keeps state of pause menu
        public static bool Pause { private set; get; } = false;

        public static bool Hub { set; get; } = true;

        //this will be called from server when we receive all the neccesary info about game properties like gamemode
        public delegate void OnPlayerJoined(Gamemode gamemode, NetworkIdentity player);
        public static OnPlayerJoined ClientEvent_OnJoinedToGame { get; set; }


        //reference to GamemodeUI, In scene UI objects can listen on events that are included in this class that are related to gamemode events
        public static UIGamemode GamemodeUI;

        //reference to player instance, one that is owned by local player
        public static PlayerInstance ClientPlayerInstance;

        //character that is owned by local playerInstance
        public static CharacterInstance OwnedCharacterInstance { private set; get; }

        //reference to currently spectated character in fpp view
        public static CharacterInstance ObservedCharacterInstance;


        ///<summary>
        ///This number is increased by 1 every time any UI element needs to show cursor (showing pause menu, showing chat etc)and decreased by 1 every time those elements dont need anymore cursor. If this number is equal to zero, then it
        ///means that curson invisible and locked, and player can control character, move, shoot etc. If number is greater than 0
        ///than curson is shown and player controller cannot be controlled. Thanks to this approach we can stack on top of each other 
        ///prompts that require cursor, and keep track of when player character should be able to be controllable again
        ///</summary>
        static int cursorRequests = 0;

        public delegate void AccessCodeReceived(string code);
        public static AccessCodeReceived ClientEvent_OnAccessCodeReceived;

        #region team managament
        public delegate void OnAssignedToTeam(int team);

        /// <summary>
        /// Event invoked when currently spectated player get assigned to team, UI components can listen to it, for example it it now 
        /// used to color hud accordingly to player team
        /// </summary>
        public static OnAssignedToTeam ClientEvent_OnAssignedToTeam { get; set; }

        public static int ThisClientTeam { private set; get; } = -1;
        public static bool ClientTeamAssigned;
        #endregion

        public static void ShowCursor(bool show)
        {
            cursorRequests = show ? cursorRequests + 1 : cursorRequests - 1;

            if (cursorRequests < 0) cursorRequests = 0;

            Cursor.visible = cursorRequests != 0;

            if (cursorRequests != 0)
                Cursor.lockState = CursorLockMode.Confined;
            else
                Cursor.lockState = CursorLockMode.Locked;
        }

        /// <summary>
        /// If true, player can be controlled, for example, when pause menu is shwoing, it is false, so player cannot move
        /// or look around
        /// </summary>
        public static bool GamePlayInput()
        {
            return cursorRequests == 0;
        }

        public static void SetPause(bool pause)
        {
            Pause = pause;
        }


        public delegate void OnObservedCharacterSet(CharacterInstance characterInstance);
        public static OnObservedCharacterSet ClientFrontendEvent_OnObservedCharacterSet { get; set; }
        //sets currently controlled or spectated character so UI elements can keep track of it
        public static void SetObservedCharacter(CharacterInstance characterInstance)
        {
            if (ObservedCharacterInstance)
            {
                ObservedCharacterInstance.IsObserved = false;
                ObservedCharacterInstance.SetFppPerspective(false);
            }

            ObservedCharacterInstance = characterInstance;

            SetClientTeam(ObservedCharacterInstance.Health.Team);

            ObservedCharacterInstance.IsObserved = true;
            ObservedCharacterInstance.SetFppPerspective(true);

            ClientFrontendEvent_OnObservedCharacterSet?.Invoke(ObservedCharacterInstance);

        }

        /// <summary>
        /// Returns Mirror's netID of character that is currently controlled (or spectated), by player
        /// </summary>
        public static uint ObservedCharacterNetID()
        {
            if (ObservedCharacterInstance)
            {
                return ObservedCharacterInstance.netId;
            }
            else
                return uint.MaxValue;
        }

        //sets currently controlled character so UI elements can keep track of it
        public static void SetOwnedCharacter(CharacterInstance characterInstance) 
        {
            OwnedCharacterInstance = characterInstance;
        }

        /// <summary>
        /// By this method we remember team of our player, so I can color itself accordingly
        /// </summary>
        public static void SetClientTeam(int team) 
        {
            ClientTeamAssigned = team != -1;
            ThisClientTeam = team;
            ClientEvent_OnAssignedToTeam?.Invoke(team);
        }
    }
}