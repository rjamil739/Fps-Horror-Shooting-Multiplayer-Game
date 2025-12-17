using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiFPS
{

    public class DNTransform : NetworkBehaviour
    {
        Vector3 _lastSyncPosition;

        public bool NeedSync { private set; get; }

        int _teleported;
        uint _updateID;

        void Start()
        {
            GameTicker.Game_Tick += ClientTick;
        }
        private void OnDestroy()
        {
            GameTicker.Game_Tick -= ClientTick;
        }

        void ClientTick()
        {
            if (!isOwned) return;

            float distanceFromLastSync = Vector3.Distance(transform.position, _lastSyncPosition);

            if (distanceFromLastSync < 0.001f) return;

            NeedSync = true;
            NetworkClient.Send(new ClientSendPositionMessage { Position = ReadPositionMsg() });
        }

        public bool DoesNeedSync()
        {
            return true;
            //return 0.001f < Vector3.Distance(transform.position, _lastSyncPosition);
        }

        public Vector3 ReadPositionMsg()
        {
            NeedSync = false;
            _lastSyncPosition = transform.position;
            return transform.position;
        }

        public void ReceivePositionFromClient(Vector3 msg)
        {

            NeedSync = false;
            transform.position = msg;
        }

        public void ServerTeleport(Vector3 pos, Quaternion rot)
        {
            transform.SetPositionAndRotation(pos, rot);
            _lastSyncPosition = pos;
            NeedSync = false;

            _teleported = 6;

            Physics.SyncTransforms();
        }



        public void Teleport(Vector3 pos, Quaternion rot)
        {
            transform.SetPositionAndRotation(pos, rot);

            _lastSyncPosition = pos;
            NeedSync = false;

            Physics.SyncTransforms();
        }

        public void UpdateClient(Vector3 msg)
        {
            if (isServer) return;
            if (!isOwned)
            {
                if (_teleported > 0)
                {
                    _teleported--;
                    return;
                }

                transform.position = msg;
            }
        }
    }
}