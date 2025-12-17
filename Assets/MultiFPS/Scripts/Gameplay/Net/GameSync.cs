using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.Gamemodes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MultiFPS.Gameplay {
    public class GameSync : LobbySynchronizer
    {
        float _tickDuration;

        byte _updateCharactersInputsFrequency = 2;
        byte _updateCharactersInputsTick = 0;

        byte _updateCharactersPositionsFrequency = 1;
        byte _updateCharactersPositionsTick = 0;

        public static GameSync Singleton;

        #region Declared Pools
        public LobbySyncPool<CharacterInstance> Characters;
        public LobbySyncPool<Health> Healths;

        float _updateTimer;

        private void Awake()
        {
            Singleton = this;

            Characters = new LobbySyncPool<CharacterInstance>("CharIns", 32, this);
            Healths = new LobbySyncPool<Health>("Healths", 128, this);
        }
        #endregion

        List<CharacterPositionMsg> _positionMsgs = new List<CharacterPositionMsg>(32);

        protected void Start()
        {
            _tickDuration = 1f / DNNetworkManager.Instance.sendRate;
        }


        void Update()
        {
            if (!isServer) return;
            if (Time.time > _updateTimer)
            {
                _updateTimer = Time.time + _tickDuration;
                ServerTick();
            }
        }

        public void ServerTick()
        {
            _updateCharactersInputsTick++;

            if (_updateCharactersInputsTick >= _updateCharactersInputsFrequency)
            {
                UpdateCharacterInputs();
                _updateCharactersInputsTick = 0;
            }

            _updateCharactersPositionsTick++;

            if (_updateCharactersPositionsTick >= _updateCharactersPositionsFrequency)
            {
                UpdateCharacterPositions();
                _updateCharactersPositionsTick = 0;
            }
        }

     
        #region healths

        void UpdateCharacterInputs()
        {
            CharacterInputMessage[] inputMsgs = new CharacterInputMessage[Characters.RegisteredObjs];
            //CharacterInputMessage[] inputMsgs = new CharacterInputMessage[Characters.Obj.Length];
            int di = 0;

            for (int i = 0; i < Characters.Obj.Length; i++)
            {
                if (Characters.Obj[i] != null)
                {
                    inputMsgs.SetValue(Characters.Obj[i].ServerPrepareInputMessage(), di);
                    di++;
                }
            }

            SendToLobbyClients(new CharactersInputMessage { Inputs = inputMsgs, StateID = Characters.PoolStateHash }, Channels.Unreliable);
        }
        void UpdateCharacterPositions()
        {
            _positionMsgs.Clear();

            for (int i = 0; i < Characters.Obj.Length; i++)
            {
                if (Characters.Obj[i] != null)
                {
                    if (Characters.Obj[i].DnTransform.DoesNeedSync())
                    {
                        Characters.Obj[i].DnTransform.ReadPositionMsg();
                        _positionMsgs.Add(new CharacterPositionMsg(Characters.Obj[i]));
                    }
                }
            }

            SendToLobbyClients(new CharactersPositionMessage { Positions = _positionMsgs.ToArray(), StateID = Characters.PoolStateHash }, Channels.Unreliable);
        }



        public void ClientUpdateCharacterPositions(CharacterPositionMsg[] positions, ushort stateID)
        {
            if (isServer) return;

            if (stateID != Characters.PoolStateHash)
            {
                //old msg, dont apply
                return;
            }

            //print($"Client updated char positions: {positions.Length}");
            //update position
            for (int i = 0; i < positions.Length; i++)
            {
                CharacterInstance character = Characters.Obj[positions[i].CharacterID];

                if (character == null)
                {
                    print($"received position msg for non existing character with DNID: {positions[i].CharacterID}");
                    continue;
                }

                if (character.isOwned) continue; //dont apply position for local client

                character.PrepareCharacterToLerp();
                character.DnTransform.UpdateClient(positions[i].Position);
                character.SetCurrentPositionTargetToLerp(positions[i].Position);
            }

        }

        public void ClientUpdateCharactersInputs(CharacterInputMessage[] inputs, ushort stateID)
        {
            if (isServer) return;

            if (stateID != Characters.PoolStateHash)
            {
                //old msg
                return;
            }

            int inputID = 0;
            for (int i = 0; i < Characters.Obj.Length; i++)
            {
                if (!Characters.Obj[i]) continue;

                if (inputID >= inputs.Length) continue;

                if (!Characters.Obj[i].isOwned)
                    Characters.Obj[i].ReadAndApplyInputFromServer(inputs[inputID]);

                inputID++;
            }
        }
        #endregion


        void SendToLobbyClients<T>(T message, int channelId = Channels.Unreliable) where T : struct, NetworkMessage
        {
            NetworkServer.SendToReadyObservers(netIdentity, message, false, channelId);
        }
    }
}