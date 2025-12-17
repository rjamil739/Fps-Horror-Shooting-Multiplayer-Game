using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiFPS {
    public class GameplayCamera : MonoBehaviour
    {
        public static GameplayCamera _instance;
        private Transform target;

        private Camera _camera;
        public Camera FPPCamera;

        private float _fovMultiplier = 1f;
        private float _rawRequestedFOV;


        private void Awake()
        {
            _instance = this;

            if (!target)
            {
                target = transform;
            }

            _camera = GetComponent<Camera>();

            _rawRequestedFOV = UserSettings.FieldOfView;
        }
        private void Update()
        {
            if (target)
                transform.SetPositionAndRotation(target.position, target.rotation);

            float finalFOV = _rawRequestedFOV * _fovMultiplier;

            _camera.fieldOfView = finalFOV;

            FPPCamera.fieldOfView = UserSettings.FieldOfViewFppModels;
        }
        public void SetTarget(Transform _target)
        {
            if (!_target)
                return;

            target = _target;
        }

        public void MultiplyMovementFieldOfView(float _fieldOfViewMultiplier, float _speed = 5f)
        {
            _fovMultiplier = Mathf.Lerp(_fovMultiplier, _fieldOfViewMultiplier, _speed * Time.deltaTime);
        }
        public void SetFieldOfView(float _fieldOfView)
        {
            _rawRequestedFOV = _fieldOfView;
        }

        public void SetFovToDefault() 
        {
            _rawRequestedFOV = UserSettings.FieldOfView;
        }
    }

}
