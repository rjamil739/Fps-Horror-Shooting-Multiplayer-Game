using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiFPS.Gameplay
{
    /// <summary>
    /// It's only responsible for visual camera recoil (on shooting and receiving damage)
    /// Weapons spray is calculated in weapons classes
    /// </summary>
    public class PlayerRecoil : MonoBehaviour
    {
        [SerializeField] Transform _recoilObject;
        Coroutine _CurrentRecoilCoroutine;
        CharacterInstance _characterInstance;

        public Vector3 MovementFactor;
        float _movementTimer;
        float RunningSpeed = 4f;
        float WalkingSpeed = 3f;
        float RunningStrength = 0.055f;
        float WalkingStrength = 0.008f;
        float ComingBackSpeed = 0.2f;


        float _fallingTime = 0;

        Vector3 finalAirPosition;
        bool isFalling = false;
        private Coroutine _hittedGroundProcedure;
        

        private void Update()
        {
            if (_characterInstance)
            {
                if ((_characterInstance.IsRunning  || _characterInstance.Input.Movement != Vector2.zero)
                    && _characterInstance.Health.CurrentHealth > 0
                    && !_characterInstance.Block)
                {
                    _movementTimer += Time.deltaTime * 180;

                    float speed = _characterInstance.IsRunning ? RunningSpeed : WalkingSpeed;

                    MovementFactor.x = Mathf.Sin(_movementTimer * Mathf.Deg2Rad * speed) * 1.5f;
                    MovementFactor.y = Mathf.Sin(_movementTimer * Mathf.Deg2Rad * 2 * speed);
                    MovementFactor *= _characterInstance.IsRunning ? RunningStrength : WalkingStrength;
                }
                else
                {
                    _movementTimer = 0;
                    MovementFactor = Vector3.MoveTowards(MovementFactor, Vector3.zero, ComingBackSpeed * Time.deltaTime);
                }

                _recoilObject.transform.localPosition = MovementFactor + new Vector3(0,0,finalAirPosition.z);
                _recoilObject.transform.position += new Vector3(0, finalAirPosition.y, 0);

                if (!_characterInstance.isGrounded)
                {
                    _fallingTime += Time.deltaTime;
                    isFalling = true;
                }
                else if (isFalling)
                {
                    isFalling = false;

                    if (_hittedGroundProcedure != null)
                        StopCoroutine(_hittedGroundProcedure);
                    StartCoroutine(HittedGroundProcedure(Mathf.Clamp(_fallingTime*1.25f, 1f,2f)));

                    _fallingTime = 0;
                }
            }
        }

        IEnumerator HittedGroundProcedure(float fallDurationFactor)
        {
            float force = 0.34f* fallDurationFactor;
            Vector3 startPos = finalAirPosition;
            Vector3 posUp = new Vector3(0, -1.5f*force, -force * 0.5f);

            float timeNeeded = 0.09f;//(posUp.y - finalAirPosition.y) / hitAnimSpeed;
            float timeNeeded2 = 0.13f* fallDurationFactor;//(force * 0.5f) / hitAnimSpeed;

            float timer = 0;

            while ((timeNeeded + timeNeeded2) >= timer)
            {
                timer += Time.deltaTime;
                if (timer <= timeNeeded)
                {
                    finalAirPosition = Vector3.Slerp(startPos, posUp, timer / timeNeeded);
                }
                else
                {
                    finalAirPosition = Vector3.Slerp(posUp, Vector3.zero, (timer - timeNeeded) / timeNeeded2);
                }
                yield return null;
            }

            finalAirPosition = Vector3.zero;
        }

        public void Initialize(Transform recoilObject, CharacterInstance characterInstance)
        {
            if (recoilObject)
                _recoilObject = recoilObject;

            //listen to event when player receives damage to shake camera a little in such case
            _characterInstance = characterInstance;

            characterInstance.Health.Client_OnHealthStateChanged += OnReceivedDamage;
        }


        private void OnDestroy()
        {
            if (_recoilObject)
                _recoilObject = null;

            _characterInstance.Health.Client_OnHealthStateChanged -= OnReceivedDamage;
            _characterInstance = null;

            if (_CurrentRecoilCoroutine != null)
                StopCoroutine(_CurrentRecoilCoroutine);

            _CurrentRecoilCoroutine = null;
        }

        public void RecoilReset()
        {
            _recoilObject.rotation = Quaternion.identity;
        }
        public void Recoil(float _recoil, float _devation, float _speed,float _duration)
        {
            if (_CurrentRecoilCoroutine != null)
            {
                StopCoroutine(_CurrentRecoilCoroutine);
                _CurrentRecoilCoroutine = null;
            }
            _CurrentRecoilCoroutine = StartCoroutine(DoRecoil(_recoil, _devation, _speed, _duration));
        }
        IEnumerator DoRecoil(float recoilVertical, float recoilHorizontal, float speed, float duration)
        {
            Quaternion recoilRot = Quaternion.Euler(_recoilObject.localEulerAngles.x -recoilVertical, _recoilObject.localEulerAngles.y + recoilHorizontal, 0);
            float timer = 0f;

            bool doingRecoil = true;
            bool recoilDone = false;

            float comingBackDuration = 3 * duration;

            while (doingRecoil)
            {
                yield return null;

                if (!recoilDone)
                {
                    timer += Time.deltaTime;

                    if (timer < duration)
                        _recoilObject.localRotation = Quaternion.Slerp(_recoilObject.localRotation, recoilRot, (timer / duration));
                    else 
                    {
                        recoilDone = true;
                        timer = 0f;
                    }
                }
                else
                {
                    timer += Time.deltaTime;

                    if (timer < comingBackDuration)
                        _recoilObject.localRotation = Quaternion.Slerp(_recoilObject.localRotation, Quaternion.identity, (timer/comingBackDuration));
                    else
                    {
                        doingRecoil = false;
                        _recoilObject.localRotation = Quaternion.identity;
                    }
                }
            }
        }

        public void OnReceivedDamage(int currentHealth, CharacterPart damagedPart, AttackType attackType, byte attackerID)
        {
            float deviation = 0.55f;
            Recoil(Random.Range(-deviation, deviation), Random.Range(-deviation, deviation), 8, 0.06f);
        }
    }
}
