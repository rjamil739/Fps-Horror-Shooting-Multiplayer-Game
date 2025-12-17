using UnityEngine;
using Mirror;
using MultiFPS.Gameplay.Gamemodes;
using System;

namespace MultiFPS.Gameplay {

    /// <summary>
    /// This component is responsible for managing and animating character
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(Health))]
    public class CharacterInstance : DNNetworkBehaviour
    {
        [Header("Character setup")]
        //determines point to which we will attach player marker with nickname and healthbar
        public Transform CharacterMarkerPosition;

        //Model of character, we need to have access to it in order to lerp it's position beetwen positions received from the server
        public Transform CharacterParent;

        public Transform FPPLook;

        //Transform that player's camera will stick to, since Camera is external object not included in player prefab, must be children of
        //FPPLook (look above) to account for camera recoil produced with shooting from weapons
        public Transform FPPCameraTarget; 

        public float CameraHeight = 1.7f;

        //weapons recoil is affected by player movement. This variable determines how fast it will transition beetwen states,
        //Given in deegrees for second
        public float RecoilMovementFactorChangeSpeed = 4f;

        CharacterController _controller;

        public delegate void KilledCharacter(Health health);
        public KilledCharacter Server_KilledCharacter { get; set; }

        public delegate void CharacterEvent_SetAsBOT(bool _set);
        public CharacterEvent_SetAsBOT Server_SetAsBOT;

        public Transform characterMind; //this objects indicated direction character is looking at
        public Transform characterFirePoint; //this object is child of characterMind, will be used for recoil of weapons

        /// <summary>
        /// Hitbox prefab to assign to player model
        /// </summary>
        [SerializeField] GameObject _hitBoxContainerPrefab;
        HitboxSetup _hitboxes;

        [HideInInspector] public PlayerRecoil PlayerRecoil { private set; get; }
        [HideInInspector] public ToolMotion ToolMotion;

        
        public bool IsReloading; //this will be driven by synchronized events
        public bool IsCrouching;
        public bool IsUsingItem { get; set; }
        public bool IsRunning { get; set; }
        public bool IsScoping;
        public bool IsAbleToUseItem = true;
        public bool isGrounded;

        public float RecoilFactor_Movement = 1;
        public float SensitivityItemFactorMultiplier = 1f;

        public bool Block { private set; get; } = false; //determines if character can move and shoot or not, block if it is end of round

        /// <summary>
        /// Only true for character that is controlled by client, so only for player controller
        /// </summary>
        public bool IsObserved { set; get; } = false;

        /// <summary>
        /// Flag that informs us if player is set up to be viewed in 1st or 3rd person
        /// </summary>
        public bool FPP = false;

        /// <summary>
        /// Indicates if character is controlled by server or client
        /// </summary>
        public bool BOT = false;

        Health _killer;

        public CharacterItemManager CharacterItemManager { private set; get; }
        [HideInInspector] public Transform ObjectForDeathCameraToFollow;

        Vector3 _deathCameraDirection;

        #region smooth rotation and position
        [Header("Smooth position lerp")]
        float _lastSyncTime;
        float _previousTickDuration;

        float _rotationSmoothTimer;

        float _currentRotationTargetX;
        float _currentRotationTargetY;

        Vector3 _lastPositionSync;
        Vector3 _currentPositionSync;


        public float PositionLerpSpeed = 10f;
        #endregion

        //information about skins that players selected for his items
        [HideInInspector] public int[] _skinsForItems;

        public delegate void CharacterEvent_OnPerspectiveSet(bool fpp);
        public CharacterEvent_OnPerspectiveSet Client_OnPerspectiveSet { set; get; }


        public delegate void CharacterEvent_OnPickedupObject(string message);
        public CharacterEvent_OnPickedupObject Client_OnPickedupObject { get; set; }


        public delegate void OnDestroyed();
        public OnDestroyed Client_OnDestroyed { set; get; }
        public DNTransform DnTransform { get; internal set; }

        ClientSendInputMessage SendInputMessage;
        CharacterInputMessage InputMessage;
        public CharacterInput Input;

        public Health Health { private set; get; }

        bool _spawned = false;

        [HideInInspector] public CharacterAnimator CharacterAnimator;

        private void Awake()
        {
            Health = GetComponent<Health>();
            Health.Client_OnHealthAdded += OnClientHealthAdded;
            Health.Server_OnHealthDepleted += ServerDeath;
            Health.Server_Resurrect = OnServerResurrect;
            Health.Client_OnHealthStateChanged += ClientOnHealthStateChanged;
            Health.Client_Resurrect = OnClientResurrect;

            PlayerRecoil = GetComponent<PlayerRecoil>();
            if (!PlayerRecoil)
                PlayerRecoil = gameObject.AddComponent<PlayerRecoil>();

            PlayerRecoil.Initialize(FPPCameraTarget, this);

            CharacterItemManager = GetComponent<CharacterItemManager>();
            CharacterItemManager.Setup();

            DnTransform = GetComponent<DNTransform>();
            _controller = GetComponent<CharacterController>();

            CharacterAnimator = GetComponent<CharacterAnimator>();

            GameManager.SetLayerRecursively(CharacterParent.gameObject, 8);
            SetFppPerspective(false);
        }

        protected void Start()
        {
            _lastPositionSync = transform.position;
            _currentPositionSync = transform.position;

            if (_hitBoxContainerPrefab)
            {
                _hitboxes = Instantiate(_hitBoxContainerPrefab, transform.position, transform.rotation).GetComponent<HitboxSetup>();
                _hitboxes.SetHiboxes(CharacterParent.gameObject, Health);
                Destroy(_hitboxes.gameObject);//at this point empty object
            }

            GameTicker.Game_Tick += CharacterInstance_Tick;

            Input.LookY = transform.eulerAngles.y; //assigning start look rotation to spawnpoint rotation

            gameObject.layer = 6; //setting apppropriate layer for character collisions

            if (isServer)
            {
                CharacterItemManager.SpawnStarterEquipment();
                CharacterItemManager.ServerCommandTakeItem(0);
                GameSync.Singleton.Characters.ServerRegisterDNSyncObj(this);
                _spawned = true;
            }
        }
        void Update()
        {
            #region killcam
            if (Health.CurrentHealth <= 0)
            {
                if (_killer)
                    FPPLook.rotation = Quaternion.Lerp(FPPLook.rotation, Quaternion.LookRotation(_killer.GetPositionToAttack() - FPPLook.position), 10f * Time.deltaTime);

                if (!ObjectForDeathCameraToFollow) return;

                RaycastHit hit;
                Vector3 castPosition = ObjectForDeathCameraToFollow.position + Vector3.up * 0.1f;

                float length;
                if (Physics.Raycast(castPosition, _deathCameraDirection, out hit, 5f, GameManager.environmentLayer))
                {
                    length = Mathf.Max(0, Vector3.Distance(hit.point, castPosition) - 0.5f);
                }
                else
                    length = 5f;

                FPPLook.transform.position = castPosition + _deathCameraDirection * length + transform.up * 0.2f;
                return;
            }
            #endregion

            if (CharacterItemManager.CurrentlyUsedItem)
            {
                float recoilMultiplier = !isGrounded ? 2.5f : (Input.Movement.x != 0 || Input.Movement.y != 0) ? CharacterItemManager.CurrentlyUsedItem.Recoil_walkMultiplier : 1f;
                RecoilFactor_Movement = Mathf.Lerp(RecoilFactor_Movement, recoilMultiplier, RecoilMovementFactorChangeSpeed * Time.deltaTime);
            }
            else
                RecoilFactor_Movement = 1f;

            if (Block)
                Input.Movement = Vector2.zero;


#if UNITY_EDITOR //rough 3rd person camera for debuging 3rd person animations
            //3rd person camera for testing
            if (UnityEngine.Input.GetKeyDown(KeyCode.I) && isOwned)
            {
                SetFppPerspective(!FPP);
                GetComponent<CharacterMotor>().cameraTargetPosition = new Vector3(0.55f, 2.1f, -3f);
            }
#endif

            if (ReadActionKeyCode(ActionCodes.Trigger1))
                CharacterItemManager.Fire1();
            if (ReadActionKeyCode(ActionCodes.Trigger2))
                CharacterItemManager.Fire2();

            #region observer smooth rotation

            _rotationSmoothTimer += Time.deltaTime;
            float percentage = Mathf.Clamp(_rotationSmoothTimer / _previousTickDuration, 0, 1);

            if (Health.CurrentHealth <= 0) return;

            if (isOwned)
            {
                // Vector3 positionForThisFrame = Vector3.Lerp(_lastPositionSync, _currentPositionSync, percentage);
                //  CharacterParent.position = positionForThisFrame;
                CharacterParent.position = Vector3.Lerp(CharacterParent.position, _currentPositionSync, Time.deltaTime * PositionLerpSpeed);
            }
            else
            {
                CharacterParent.position = Vector3.Lerp(CharacterParent.position, _currentPositionSync, Time.deltaTime * PositionLerpSpeed);
            }

            if (isOwned || isServer && BOT)
            {
                Input.LookX = Mathf.Clamp(Input.LookX, -90f, 90f);

                //rotate character based on player mouse input/bot input
                if (Health.CurrentHealth > 0)
                    transform.rotation = Quaternion.Euler(0, Input.LookY, 0);

                //rotate camera based on player mouse input/bot input
                FPPLook.localRotation = Quaternion.Euler(Input.LookX, 0, 0);
            }
            else
            {
                FPPLook.transform.localRotation = Quaternion.Lerp(FPPLook.transform.localRotation, Quaternion.Euler(_currentRotationTargetX, 0, 0), percentage);
                if (Health.CurrentHealth > 0)
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, _currentRotationTargetY, 0), percentage);
            }

            #endregion
        }

        

        #region position lerp
        public void PrepareCharacterToLerp() 
        {
            _lastPositionSync = CharacterParent.position;
        }
        public void SetCurrentPositionTargetToLerp(Vector3 target) 
        {
            CharacterParent.position = _lastPositionSync;

            _currentPositionSync = target;


            _previousTickDuration = Time.time - _lastSyncTime;
            _lastSyncTime = Time.time;

            _rotationSmoothTimer = 0f;
        }
        #endregion

        public bool IsClientOrBot() { return isOwned || isServer && BOT; }


        #region input networking
        void CharacterInstance_Tick()
        {
            if (isOwned && !isServer)
                NetworkClient.Send(PrepareInputMessage(), Channels.Unreliable);
        }
        #endregion


        private void ServerDeath(CharacterPart hittedPartID, AttackType attackType, Health killer, int attackForce) 
        {
            if (killer)
            {
                CharacterInstance killerChar = killer.GetComponent<CharacterInstance>();

                if (killerChar)
                    killerChar.Server_KilledCharacter?.Invoke(Health);
            }

            GameManager.Gamemode.Server_OnPlayerKilled(Health, killer);
            _controller.enabled = false;
        }

        private void OnServerResurrect(int health) 
        {
            _lastPositionSync = transform.position;

            _controller.enabled = true;

            if (_spawned)
            {
                CharacterItemManager.SpawnStarterEquipment();
                CharacterItemManager.ServerCommandTakeItem(0);
            }
        }
        private void OnClientResurrect(int health)
        {
            //disable collider for dead character
            _controller.enabled = true;
            FPPLook.transform.localPosition = new Vector3(0, CameraHeight, 0);
            SetFppPerspective(false);
        }
        public void ClientOnHealthStateChanged(int currentHealth, CharacterPart hittedPartID, AttackType attackType, byte attackerID)
        {
            if (currentHealth > 0) return;

            //death
            _controller.enabled = false;

            //Set camera to follow killer
            if (IsObserved)
            {
                GameplayCamera._instance.SetFovToDefault();

                _killer = GameSync.Singleton.Healths.GetObj(attackerID);

                if (_killer)
                {
                    _deathCameraDirection = _killer.netId != netId ?
                    (FPPLook.transform.position - _killer.GetPositionToAttack()).normalized :
                    -FPPLook.forward;

                    if (_killer && (attackerID != DNID)) //dont show this message in case of suicide
                        GameManager.GameEvent_GamemodeEvent_Message?.Invoke("You were killed by " + _killer.CharacterName, RoomSetup.Properties.P_RespawnCooldown);
                }
            }
        }

        public void BlockCharacter(bool block) 
        {
            Block = block;
            RpcBlockCharacter(block);
        }
        [ClientRpc]
        private void RpcBlockCharacter(bool block) 
        {
            Block = block;
        }

        public void SetFppPerspective(bool fpp) 
        {
            FPP = fpp;

            Client_OnPerspectiveSet?.Invoke(fpp);

            if (fpp)
                GameplayCamera._instance.SetTarget(FPPCameraTarget);
        }


        #region input
        public bool ReadActionKeyCode(ActionCodes actionCode)
        {
            return (Input.ActionCodes & (1 << (int)actionCode)) != 0;
        }
        public void SetActionKeyCode(ActionCodes actionCode, bool _set)
        {
            int a = Input.ActionCodes;
            if (_set)
            {
                a |= 1 << ((byte)actionCode);
            }
            else
            {
                a &= ~(1 << (byte)actionCode);
            }
            Input.ActionCodes = (byte)a;
        }
        #endregion

        public void SetAsBOT(bool _set)
        {
            //if turn bot to player, game will try to teleport him to his spawnpoint because server does not write these values
            //for bots every single tick
            if (BOT)
            {
                _lastPositionSync = transform.position;
                _currentPositionSync = transform.position;
            }

            BOT = _set;

            Server_SetAsBOT?.Invoke(_set);
        }


        //for ui to read this and display message in hud
        void OnClientHealthAdded(int currentHealth, int addedHealth, byte healerID)
        {
            Client_OnPickedupObject?.Invoke($"Health +{addedHealth}");
        }

        public void ServerTeleport(Vector3 pos, float rotationY) 
        {
            Input.LookX = 0;
            Input.LookY = rotationY;

            transform.SetPositionAndRotation(pos, Quaternion.Euler(0, rotationY, 0));
            CharacterParent.localPosition = Vector3.zero;
            _lastPositionSync = pos;
            _currentPositionSync = pos;
            Physics.SyncTransforms();
            RpcTeleport(pos, rotationY);
        }

        [ClientRpc]
        public void RpcTeleport(Vector3 pos, float rotationY) 
        {
            if (isServer) return;

            Input.LookX = 0;
            Input.LookY = rotationY;

            _lastPositionSync = pos;
            _currentPositionSync = pos;
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0,rotationY,0));

            CharacterParent.localPosition = Vector3.zero;

            if(!isServer)
                Physics.SyncTransforms();
        }


        public ClientSendInputMessage PrepareInputMessage()
        {
            SendInputMessage.Movement = FitMovementInputToOneByte(Input.Movement);
            SendInputMessage.LookX = (sbyte)Mathf.FloorToInt(Input.LookX);
            SendInputMessage.LookY = (short)Input.LookY;
            SendInputMessage.ActionCodes = Input.ActionCodes;

            return SendInputMessage;
        }
        public CharacterInputMessage ServerPrepareInputMessage()
        {
            InputMessage.Movement = FitMovementInputToOneByte(Input.Movement);
            InputMessage.LookX = (sbyte)Mathf.FloorToInt(Input.LookX);
            InputMessage.LookY = (short)Input.LookY;
            InputMessage.ActionCodes = Input.ActionCodes;

            return InputMessage;
        }


        public byte FitMovementInputToOneByte(Vector2 movement)
        {
            // Two small signed numbers (values between -8 to 7)
            int mX = Mathf.FloorToInt(movement.x / 0.2f);
            int mY = Mathf.FloorToInt(movement.y / 0.2f);

            // Convert the numbers to 4-bit two's complement representation
            byte first4Bits = (byte)((mX < 0 ? 0x08 : 0x00) | (Math.Abs(mX) & 0x07)); // Check sign bit and keep last 3 bits
            byte second4Bits = (byte)((mY < 0 ? 0x08 : 0x00) | (Math.Abs(mY) & 0x07)); // Check sign bit and keep last 3 bits

            // Combine the two 4-bit representations into a single byte
            return (byte)((first4Bits << 4) | second4Bits);
        }

        public void ReadMovementInputFromByte(byte input)
        {
            Input.Movement.x = ((input & 0x70) >> 4) * ((input & 0x80) == 0x80 ? -1 : 1) * 0.2f;
            Input.Movement.y = (input & 0x07) * ((input & 0x08) == 0x08 ? -1 : 1) * 0.2f;
        }

        public void ReadAndApplyInputFromMessage(ClientSendInputMessage msg)
        {
            ApplyInput(msg.Movement, msg.LookX, msg.LookY, msg.ActionCodes);
        }
        public void ReadAndApplyInputFromServer(CharacterInputMessage msg)
        {
            ApplyInput(msg.Movement, msg.LookX, msg.LookY, msg.ActionCodes);
        }

        void ApplyInput(byte movementInput, float lookInputX, float lookInputY, byte actionCodes) 
        {
            ReadMovementInputFromByte(movementInput);
            Input.LookX = lookInputX;
            Input.LookY = lookInputY;
            Input.ActionCodes = actionCodes;

            _currentRotationTargetX = lookInputX;
            _currentRotationTargetY = lookInputY;
        }


        protected override void OnDisable()
        {
            base.OnDisable();
            Client_OnDestroyed?.Invoke();

            Health.Server_OnHealthDepleted -= ServerDeath;
            Health.Client_OnHealthStateChanged -= ClientOnHealthStateChanged;

            GameTicker.Game_Tick -= CharacterInstance_Tick;
        }

        void OnDestroy()
        {
            if(GameSync.Singleton)
                GameSync.Singleton.Characters.ServerDeregisterDNSyncObj(this);

            characterFirePoint = null;
            FPPCameraTarget = null;
            FPPLook = null;
            CharacterParent = null;
            CharacterMarkerPosition = null;
            characterMind = null;
            characterFirePoint = null;

            PlayerRecoil = null;
            CharacterItemManager = null;
            ToolMotion = null;
        }
    }
    public enum ActionCodes 
    {
        Trigger1,
        Trigger2,
        Sprint,
        Crouch,
    }

    [System.Serializable]
    //input that is interpreted by the game
    public struct CharacterInput
    {
        public Vector2 Movement;
        public float LookX;
        public float LookY;
        public byte ActionCodes;
    }

    //compressed version of input that will be sent over network
    public struct CharacterInputMessage
    {
        public byte Movement; //both horizontal and vertical movement input will be stored in one byte
        public sbyte LookX;
        public short LookY;
        public byte ActionCodes; //sprint, fire1, fire2, and 5 free inputs to utilize   
    }
}
