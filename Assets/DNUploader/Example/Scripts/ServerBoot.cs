using System;
using UnityEngine;
using Mirror;

namespace DNUploader.Examples
{
    public class ServerBoot : MonoBehaviour
    {
        void Start()
        {
            string[] commands = Environment.GetCommandLineArgs();

            for (int i = 0; i < commands.Length; i++)
            {
                if (commands[i] == "server")
                {
                    ushort port = 3000;
                    if (commands.Length > i + 1) 
                    {
                        try
                        {
                            port = System.Convert.ToUInt16(commands[i + 1]);
                        }
                        catch 
                        {
                            Debug.LogError($"Port was provided with incorrect format, server will be listening on port {port} instead");
                        }
                    }

                    //start server
                    NetworkManager.singleton.GetComponent<kcp2k.KcpTransport>().Port = port;
                    NetworkManager.singleton.StartServer();

                    Debug.Log($"SERVER IS LISTENING ON {port}");
                }
            }
        }
    }
}