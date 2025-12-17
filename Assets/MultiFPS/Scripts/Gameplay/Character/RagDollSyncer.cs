using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using MultiFPS.Gameplay.Gamemodes;
using System;

namespace MultiFPS.Gameplay {
    public class RagDollSyncer : NetworkBehaviour
    {

        RagDoll _ragDoll;

        bool serverIsSynchronizing = false;
        Quaternion[] _rigidBodyRotations = new Quaternion[7];
        float[] _limbFlexors = new float [4];
        sbyte[] _limbFlexorsByte = new sbyte[4];
        Vector3 _hipsPosition;
        public float RagdollLerpSpeed = 5f;
        const float _byteAngleMultiplier = 360f/255f;

        Health _health;

        Vector3 _lastPos;

        bool _clientLerp;

        private void Start()
        {
            _health = GetComponent<Health>();
            _health.Server_Resurrect += Server_OnResurrect;
            _health.Client_Resurrect += Client_Resurrect;
        }

        void Server_OnResurrect(int health)
        {
            serverIsSynchronizing = false;
            StopAllCoroutines();
        }

        void Client_Resurrect(int health) 
        {
            _clientLerp = false;
        }


        public void ServerStartSynchronizingRagdoll(RagDoll ragdoll) 
        {
            AssignRagdoll(ragdoll);

            serverIsSynchronizing = true;
            StartCoroutine(SendRagdollInfoCoroutine());
            IEnumerator SendRagdollInfoCoroutine() 
            {
                SendRagdollInfo();
                float disableTimer = float.MaxValue;
                while (serverIsSynchronizing && _health.CurrentHealth<=0) 
                {
                    //update ragdoll for clients 10 times per second
                    yield return new WaitForSeconds(0.05f);

                    //stop synchronizing when ragdoll is steady to save bandwidth
                    if (Vector3.Distance(_lastPos, _ragDoll.SynchronizedRigidBodies[0].position) > 0.01f)
                        disableTimer = Time.time + 2f;

                    //disable ragdoll after 2 seconds of being steady
                    if (Time.time > disableTimer)
                        serverIsSynchronizing = false;

                    _lastPos = _ragDoll.SynchronizedRigidBodies[0].position;

                    SendRagdollInfo();
                }

                _ragDoll.EnablePhysics(false);  
            }
        }
        void SendRagdollInfo()
        {
            if (!serverIsSynchronizing) return;

            for (int i = 0; i < _ragDoll.SynchronizedRigidBodies.Length; i++)
                _rigidBodyRotations[i] = _ragDoll.SynchronizedRigidBodies[i].rotation;

            for (int i = 0; i < _limbFlexorsByte.Length; i++)
                _limbFlexorsByte[i] = (sbyte)Mathf.FloorToInt(_ragDoll.SynchronizedLimbFlexors[i].localEulerAngles.x / _byteAngleMultiplier);

            RpcReceiveRagdollInfo(_rigidBodyRotations, _ragDoll.SynchronizedRigidBodies[0].position, _limbFlexorsByte);
        }
        //receive ragdoll data from server
        [ClientRpc(channel = Channels.Unreliable)]
        void RpcReceiveRagdollInfo(Quaternion[] rigidBodiesRotations, Vector3 hipsPosition, sbyte[] limbFlexors)
        {
            if (!_ragDoll)
                return;

            if (serverIsSynchronizing) return;

            _clientLerp = true;

            //pose ragdoll as server says
            _hipsPosition = hipsPosition;
            _rigidBodyRotations = rigidBodiesRotations;

            for (int i = 0; i < limbFlexors.Length; i++)
            {
                _limbFlexors[i] = limbFlexors[i] * _byteAngleMultiplier;
            }
        }

        //lerp ragdoll to state received from server
        private void Update()
        {
            if (isServer) return;

            if (_ragDoll == null) return;

            if (!_clientLerp) return;

            //lerp ragdoll position
            _ragDoll.SynchronizedRigidBodies[0].position = Vector3.Lerp(
                _ragDoll.SynchronizedRigidBodies[0].position,
                _hipsPosition, 
                RagdollLerpSpeed * Time.deltaTime
            );

            //lerp limbs rotations
            for (int i = 0; i < _ragDoll.SynchronizedRigidBodies.Length; i++)
            {
                _ragDoll.SynchronizedRigidBodies[i].rotation = Quaternion.Lerp(
                    _ragDoll.SynchronizedRigidBodies[i].rotation,
                    _rigidBodyRotations[i],
                    RagdollLerpSpeed * Time.deltaTime);
            }

            //lerp joints
            for (int i = 0; i < _ragDoll.SynchronizedLimbFlexors.Length; i++)
            {
                _ragDoll.SynchronizedLimbFlexors[i].localRotation = Quaternion.Lerp(
                    _ragDoll.SynchronizedLimbFlexors[i].localRotation,
                    Quaternion.Euler(_limbFlexors[i], 0, 0),
                    RagdollLerpSpeed * Time.deltaTime
                );
            }
        }


        public void AssignRagdoll(RagDoll ragDoll) 
        {
            _ragDoll = ragDoll;
        }

        private void OnDestroy()
        {
            _ragDoll = null;
        }
    }
}