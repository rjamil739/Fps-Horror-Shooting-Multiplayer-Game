using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DNUploader.Examples
{
    public class Player : NetworkBehaviour
    {
        CharacterController _controller;

        [SerializeField] MeshRenderer _playerModel;

        [SerializeField] float _speed = 5;

        [SyncVar(hook = nameof(RpcSetColor))]
        Color _myColor;

        void Start()
        {
            _controller = GetComponent<CharacterController>();

            if (isServer)
                _myColor = new Color(Random.Range(0, 2), Random.Range(0, 1f), Random.Range(0, 2));
        }

        private void FixedUpdate()
        {
            if (!isOwned) return;

            Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            _controller.Move(_speed * Time.deltaTime * move);
        }

        void RpcSetColor(Color old, Color color)
        {
            _playerModel.material.color = color;
        }
    }
}