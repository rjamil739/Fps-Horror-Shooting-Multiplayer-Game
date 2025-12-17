using kcp2k;
using Mirror.SimpleWeb;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MultiFPS {

    public class DNTransportImplementer : MonoBehaviour
    {
        DNNetworkManager _manager;

        public Transports Transport;

        KcpTransport _t_kcp;
        SimpleWebTransport _t_simpleWebTransport;

        void Awake()
        {
            _manager = GetComponent<DNNetworkManager>();
            _manager.Action_SetNetworkManagerPort += SetPort;

            _t_kcp = GetComponent<KcpTransport>();
            _t_simpleWebTransport = GetComponent<SimpleWebTransport>();

            switch (Transport)
            {
                case Transports.KCP:
                    _manager.transport = _t_kcp;
                    break;
                case Transports.SimpleWebTransport:
                    _manager.transport = _t_simpleWebTransport;
                    break;
            }
        }

        void SetPort(ushort port)
        {
            switch (Transport) 
            {
                case Transports.KCP:
                    _manager.GetComponent<KcpTransport>().port = port;
                    break;
                case Transports.SimpleWebTransport:
                    _manager.GetComponent<SimpleWebTransport>().port = port;
                    break;
            }
        }

        public enum Transports
        {
            KCP,
            SimpleWebTransport,
        }
    }
}