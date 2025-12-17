using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiFPS
{
    public class LobbySynchronizer : DNNetworkBehaviour
    {
        byte _poolCount = 0;
        Dictionary<byte, LobbySyncPoolBase> _registeredPools = new Dictionary<byte, LobbySyncPoolBase>();

        public byte RegisterPool(LobbySyncPoolBase pool)
        {
            _poolCount++;
            _registeredPools.Add(_poolCount, pool);
            return _poolCount;
        }

        
        //network methods
        protected override sealed void OnNewPlayerConnected(NetworkConnection conn)
        {
            StartCoroutine(setDelayed());
            IEnumerator setDelayed()
            {
                yield return new WaitForEndOfFrame();
                LobbySyncPoolBase[] pools = new List<LobbySyncPoolBase>(_registeredPools.Values).ToArray();
                InitListMsg msg = new InitListMsg();
                msg.Pools = new SingleSynchronizedListMsg[pools.Length];

                for (int i = 0; i < pools.Length; i++)
                {
                    msg.Pools[i] = pools[i].ConvertToMsg();
                }
                TargetRpcClientInitList(conn, msg);
            }
        }

        [TargetRpc]
        void TargetRpcClientInitList(NetworkConnection conn, InitListMsg msg) 
        {
            if (isServer) return;


            for (int i = 0; i < msg.Pools.Length; i++)
                _registeredPools[msg.Pools[i].PoolID].RpcInitDNList(msg.Pools[i].Entities, msg.Pools[i].Hash);
        }

        [ClientRpc]
        internal void ClientRegisterMsg(RpcRegisterDNSyncObjectMsg msg)
        {
            if (isServer) return;


            if (NetworkClient.spawned.TryGetValue(msg.MirrorNetID, out NetworkIdentity id))
                _registeredPools[msg.PoolID].RpcRegisterDNSyncObj(id, msg.DNID, msg.StateHash);
            else
                Debug.LogWarning($"Tried to register obj that was not yet spawned: {msg.PoolID}, netid: {msg.MirrorNetID}");
        }
        [ClientRpc]
        public void ClientDeregisterObj(RpcDeregisterDNSyncObjectMsg msg)
        {
            if (isServer) return;
            //if (NetworkClient.spawned.TryGetValue(msg.MirrorNetID, out NetworkIdentity id))
            _registeredPools[msg.PoolID].RpcDeregisterDNSyncObj(msg.DNID, msg.StateHash);
        }
        
    }

    public struct RpcRegisterDNSyncObjectMsg : NetworkMessage
    {
        public uint MirrorNetID;
        public byte PoolID;
        public byte DNID;
        public ushort StateHash;
    }
    public struct RpcDeregisterDNSyncObjectMsg : NetworkMessage
    {
        public uint MirrorNetID;
        public byte PoolID;
        public byte DNID;
        public ushort StateHash;
    }

    [System.Serializable]
    public class LobbySyncPool<T> : LobbySyncPoolBase where T : class
    {
        LobbySynchronizer _synchronizer;

        public T[] Obj;

        public LobbySyncPool(string name, byte maxSize, LobbySynchronizer lobbySynchronizer)
        {
            _poolName = name;
            _maxSize = maxSize < 255 ? maxSize : (byte)254;

            _netIDs = new NetworkIdentity[maxSize];
            Obj = new T[maxSize];

            _poolID = lobbySynchronizer.RegisterPool(this);

            _synchronizer = lobbySynchronizer;
        }

        public override void ServerRegisterDNSyncObj(DNNetworkBehaviour behaviour)
        {
            if (!_synchronizer.isServer) return;

            PoolStateHash++;
            if (PoolStateHash == ushort.MaxValue)
                PoolStateHash = 0;

            RegisteredObjs++;

            for (byte i = 0; i < _maxSize; i++)
            {
                if (_netIDs[i] != null) continue;

                _netIDs.SetValue(behaviour.netIdentity, i);
                Obj.SetValue(behaviour.GetComponent<T>(), i);

                DNNetworkBehaviour[] dnBeh = behaviour.GetComponents<DNNetworkBehaviour>();

                for (int j = 0; j < dnBeh.Length; j++)
                {
                    if (dnBeh[j].GetType() == typeof(T))
                        dnBeh[j].SetDNID(i);
                }

                _synchronizer.ClientRegisterMsg(new RpcRegisterDNSyncObjectMsg
                {
                    MirrorNetID = behaviour.netId,
                    PoolID = _poolID,
                    DNID = i,
                    StateHash = PoolStateHash
                });
                return;
            }

            Debug.LogError($"Limit of registered synchronized object reached for pool: {_poolName}");
        }
        public override void ServerDeregisterDNSyncObj(DNNetworkBehaviour netObj)
        {
            if (!_synchronizer.isServer) return;

            if (netObj.DNID == byte.MaxValue) return; //it was never registered, so dont try to deregister
            if (netObj.netIdentity != _netIDs[netObj.DNID]) {
                Debug.LogWarning($"Server tried to deregister obj that was not registered: {netObj.name}");
                return;
            }

            PoolStateHash++;
            if (PoolStateHash == ushort.MaxValue)
                PoolStateHash = 0;

            RegisteredObjs--;

            _netIDs[netObj.DNID] = null;
            Obj[netObj.DNID] = null;

            _synchronizer.ClientDeregisterObj(new RpcDeregisterDNSyncObjectMsg
            {
                MirrorNetID = netObj.netId,
                PoolID = _poolID,
                DNID = netObj.DNID,
                StateHash = PoolStateHash
            });
        }

        public override void RpcRegisterDNSyncObj(NetworkIdentity netID, byte id, ushort hash)
        {
            PoolStateHash = hash;

            if (_netIDs[id] != null)
            {
                Debug.LogWarning($"Client tried to register obj in occupied slot {id}: {netID.name}");
            }

            _netIDs[id] = netID;
            Obj.SetValue(netID.GetComponent<T>(), id);

            DNNetworkBehaviour[] dnBeh = netID.GetComponents<DNNetworkBehaviour>();

            for (int i = 0; i < dnBeh.Length; i++)
            {
                //Debug.LogWarning($"{dnBeh[i].GetType()} = {typeof(T)} {dnBeh[i].GetType() == typeof(T)}");

                if (dnBeh[i].GetType() == typeof(T))
                {
                    dnBeh[i].SetDNID(id);
                    //Debug.LogWarning($"$ID {id} SET FOR {dnBeh[i].GetType()} on gameObject: {dnBeh[i].gameObject.name}");
                }

            }
        }
        public override void RpcDeregisterDNSyncObj(byte dnid, ushort hash)
        {
            NetworkIdentity netID = _netIDs[dnid];

            PoolStateHash = hash;

            if (_netIDs[dnid] != netID)
            {
                Debug.LogWarning($"Client tried to deregister obj that was not registered: {netID.name}");
                return;
            }

            _netIDs[dnid] = null;
            Obj[dnid] = null;
        }

        #region update late player
        public override void RpcInitDNList(SingleSynchronizedDnEntityMsg[] entities, ushort hash)
        {
            PoolStateHash = hash;
            for (int i = 0; i < entities.Length; i++)
            {
                SingleSynchronizedDnEntityMsg entity = entities[i];
                NetworkIdentity mNetID = NetworkClient.spawned[entity.MirrorID];

                _netIDs[entity.DNID] = mNetID;
                Obj[entity.DNID] = mNetID.GetComponent<T>();

                DNNetworkBehaviour[] dnBeh = mNetID.GetComponents<DNNetworkBehaviour>();

                for (int j = 0; j < dnBeh.Length; j++)
                {
                    //Debug.LogWarning($"{dnBeh[j].GetType()} = {typeof(T)} {dnBeh[j].GetType() == typeof(T)}");

                    if (dnBeh[j].GetType() == typeof(T))
                    {
                        dnBeh[j].SetDNID(entity.DNID);
                        //Debug.LogWarning($"$ID {entity.DNID} SET FOR {dnBeh[j].GetType()}");
                    }
                }
            }
        }

        public T GetObj(byte attackerID)
        {
            return Obj[attackerID];
        }
        #endregion

    }

    public class LobbySyncPoolBase 
    {
        protected string _poolName = "Default";
        protected byte _poolID = 255;
        protected byte _maxSize;

        protected NetworkIdentity[] _netIDs;

        public ushort PoolStateHash { get; protected set; }

        public byte RegisteredObjs { get; protected set; } = 0; //server only

        public virtual void ServerRegisterDNSyncObj(DNNetworkBehaviour behaviour)
        {
        }
        public virtual void ServerDeregisterDNSyncObj(DNNetworkBehaviour netObj)
        {
        }

        //entries for dictionary
        public virtual void RpcRegisterDNSyncObj(NetworkIdentity netObj, byte id, ushort hash) { }

        public virtual void RpcDeregisterDNSyncObj(byte dnid, ushort hash) { }

        public virtual void RpcInitDNList(SingleSynchronizedDnEntityMsg[] entities, ushort hash) { }

        internal SingleSynchronizedListMsg ConvertToMsg()
        {
            SingleSynchronizedListMsg msg = new SingleSynchronizedListMsg();
            msg.PoolID = _poolID;
            msg.Entities = new SingleSynchronizedDnEntityMsg[RegisteredObjs];
            msg.Hash = PoolStateHash;

            byte entityID = 0;
            
            for (byte i = 0; i < _netIDs.Length; i++)
            {
                if (_netIDs[i] == null) continue;

                msg.Entities[entityID] = new SingleSynchronizedDnEntityMsg { MirrorID = _netIDs[i].netId, DNID = i};
                entityID++;
            }

            return msg;
        }
    }
    public struct InitListMsg : NetworkMessage
    {
        public SingleSynchronizedListMsg[] Pools;
    }
    public struct SingleSynchronizedListMsg
    {
        public byte PoolID;
        public ushort Hash;
        public SingleSynchronizedDnEntityMsg[] Entities;

    }
    public struct SingleSynchronizedDnEntityMsg
    {
        public byte DNID;
        public uint MirrorID;
    }
}