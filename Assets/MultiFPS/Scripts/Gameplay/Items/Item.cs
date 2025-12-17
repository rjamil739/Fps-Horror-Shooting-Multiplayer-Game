using System.Collections;
using UnityEngine;
using Mirror;
using MultiFPS.UI.HUD;
using MultiFPS.UI;

namespace MultiFPS.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(ModelSticker))]
    [DisallowMultipleComponent]

    /// <summary>
    /// Base class for all items in the game
    /// </summary>  
    [AddComponentMenu("MultiFPS/Items/Item")]
    public class Item : NetworkBehaviour
    {
        /// <summary>
        /// its only for inventory system placed on player to check if it already has that item
        /// when player tries to grab it
        /// </summary>
        [Header("Item setup")]
        public string ItemName;
        /// <summary>
        /// item model, we need access to that so we can hide it when its aquired by player and not in use
        /// </summary>
        [SerializeField] protected GameObject ItemModel;
        [SerializeField] SkinnedMeshRenderer _itemMesh;
        [SerializeField] Mesh _lowPolyMesh;
        [SerializeField] bool _placeInHandInFPP;
        Mesh _highPolyMesh;
        [Header("Item managament")]
        public SlotType SlotType = SlotType.Normal;
        public FireMode Firemode = FireMode.Automatic;
        public bool CanBeDropped = true;

        /// <summary>
        /// if this is equal to -1, then every team can pick this item up,
        /// if this is equal to 0, only team with id 0 cab, if this is 1, then only team 1, etc
        /// </summary>
        public int TeamOwnership = -1;
        [Header("AnimationSets")]
        /// <summary>
        /// animations for player model relevant to item, if null then player model will play default "empty handed" animation set
        /// </summary>
        public RuntimeAnimatorController AnimatorControllerForCharacter;
        public RuntimeAnimatorController AnimatorControllerForCharacterFPP;
        public RuntimeAnimatorController AnimatorControllerForItem;
        public RuntimeAnimatorController AnimatorControllerForItemFPP;
        public Vector3 FPPModelOffset;

        [Header("Scope properties")]

        [SerializeField] protected bool _changeSensitivityOnScope = false;
        public float FovLerpSpeed = 20; //how fast FOV of camera wll be changed
        public float ModelLerpSpeed = 2; //how fast gun model will be centered on camera
        [SerializeField] float ScopeFov = 50;
        [SerializeField] Transform _scopePoint;

        float _scopePercantage = 0f;

        [Header("Statistics")]
        /// <summary>
        /// Time that has to pass for item to be usable since player took it 
        /// </summary>
        [SerializeField] float takingTime = 0.1f;
        /// <summary>
        /// damage that this item deals on single attack
        /// </summary>
        [SerializeField] protected int _damage = 20;


        [SerializeField] protected float coolDown = 0.5f;
        [SerializeField] float _meleeCooldown = 1f;
        [SerializeField] protected int _meleeDamage = 55;
        [SerializeField] protected int _meleeAttackForce = 1000; //how much ragdoll will be affected if killed with melee attack
        [Tooltip("Determines how much ragdoll will be affected")]
        public int AttackForce = 100;

        [Header("Ammo properties")]
        public int MaxAmmoSupply = 100;
        public int MagazineCapacity = 30;
        //for server side game logic
        public int Server_CurrentAmmoSupply;
        public int Server_CurrentAmmo { get; set; }

        //just for client feedback
        public int CurrentAmmoSupply = 100;
        public int CurrentAmmo { private set; get; }

        [Header("Audio")]
        [SerializeField] AudioClip takeClip;

        [Space]
        [Header("Crosshair")]
        //max and min size in pixels for crosshair
        public int maxSize = 256;
        public int minSize = 64;
        public bool HideWhenAiming = true;

        [Header("Recoil stats")]
        [SerializeField] public float _recoil_minAngle;
        [SerializeField] public float _recoil_maxAngle;
        [SerializeField] public float _recoil_scopeMultiplier;
        [SerializeField] protected float _recoil_angleAddedOnShot;
        [SerializeField] protected float _recoil_stabilizationSpeed; //how much angle devation from barrel to recover in one second
        [SerializeField] public float Recoil_walkMultiplier = 1.5f; //how much multiply recoil when player using item is walking
        [HideInInspector] public float _currentRecoilScopeMultiplier; //set 0 for no recoil, useful for sniper rifle
        [HideInInspector] public float CurrentRecoil;


        [Header("Recoil stats for FPP camera")]
        //recoil/camera shake properties, does not have influence for gameplay, only visual effect
        public float _recoil;
        public float _devation;
        public float _speed;
        public float _duration;
        public float _modelRecoil = 0.05f; //how much fpp model has to go back on single shot
        [Header("Aim FPP camera recoil")]
        [SerializeField] protected float _recoil_aimCamera_stabilizationSpeed = 2f;
        [SerializeField] protected float _recoil_aimCamera_recoil = 0.01f;

        [Header("UI")]
        // icon that will be displayed on killfeed if someone dies by this item
        public Sprite KillFeedIcon;
        // icon that will be displayed on player hud to inform him what he posses
        public Sprite ItemIcon;

        //events
        public delegate void OnShoot();
        public OnShoot Client_OnShoot;


        //item custom UI, for example: sniper scope for sniper rifle
        [SerializeField] GameObject _itemUI;


        protected bool _isScoping;
        /// <summary>
        /// reference to UI spawned specific for this item, only for weapons attached to player that is rendered in first person
        /// perspective
        /// </summary>
        protected HUDItem _spawnedUI;

        /// <summary>
        /// player who uses this item at the moment, if item does not belong to anyone, than it is null
        /// </summary>
        public CharacterInstance MyOwner { private set; get; }


        /// <summary>
        /// for counting cooldown
        /// </summary>
        protected float _coolDownTimer;
        protected float _meleeCoolDownTimer;

        /// player raycast interaction must reach this trigger in order to be able to pick up this item
        private SphereCollider _interactionCollider;

        //collider and rigidbody that is used for item collisions with game environment
        private BoxCollider _collider;
        private Rigidbody _rg;

        /// <summary>
        /// audio source for playing item sounds like firing clip
        /// </summary>
        protected AudioSource _audioSource;

        /// <summary>
        /// component that will stick item to hand of character model
        /// </summary>
        ModelSticker _modelSticker;

        /// <summary>
        /// animator component for animating item in game
        /// </summary>
        protected Animator _myAnimator;

        /// <summary>
        /// bunch of flags to recognize item state in game
        /// </summary>
         
       
        protected bool _currentlyInUse  { private set; get; } = false; //true if player has this item in hands
        protected bool _isReloading;
        protected bool _server_isReloading;
        protected bool _reloadTrigger = false;
        protected bool _doingMelee = false;
        bool _singleUsed = false;

        bool _itemActive;

        /// <summary>
        /// when item is dropped, we dont want it be in game world for eternity, so this coroutine will destroy it after
        /// amount of time given in static class "GameManager"
        /// </summary>
        protected Coroutine DestroyCoroutine;

        /// <summary>
        /// procedure that makes item able to be used after taking animation ends
        /// </summary>
        Coroutine takingItemProcedure;

        //for synchronizing positions of items for each client, item physics calculated on server
        NetworkTransformUnreliable _networkTransform;

        Vector3 _aimPositionRecoil;

        protected virtual void Awake()
        {
            _rg = GetComponent<Rigidbody>();
            _rg.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            _interactionCollider = GetComponent<SphereCollider>();
            _audioSource = GetComponent<AudioSource>();
            _collider = GetComponent<BoxCollider>();
            _myAnimator = ItemModel.GetComponent<Animator>();
            _modelSticker = GetComponent<ModelSticker>();
            _networkTransform = GetComponent<NetworkTransformUnreliable>();

            if(_networkTransform)
                _networkTransform.onlySyncOnChange = true;

            _myAnimator.runtimeAnimatorController = AnimatorControllerForItem;
            _interactionCollider.isTrigger = true;

            //set item layer to item layer
            gameObject.layer = 7;

            SetInteractable(true);

            CurrentAmmoSupply = MaxAmmoSupply;
            CurrentAmmo = MagazineCapacity;

            //for server logic
            Server_CurrentAmmoSupply = MaxAmmoSupply;
            Server_CurrentAmmo = MagazineCapacity;

            if (_itemMesh && _lowPolyMesh)
            {
                _highPolyMesh = _itemMesh.sharedMesh;
                _itemMesh.sharedMesh = _lowPolyMesh;
            }
        }

        private void OnDestroy()
        {
            _interactionCollider = null;
            _audioSource = null;
            _collider = null;
            _myAnimator = null;
            MyOwner = null;
            _modelSticker = null;
            _networkTransform = null;
            _rg = null;
            _itemMesh = null;
            _scopePoint = null;

            DespawnItemUIifExist();
        }

        #region Unity callbacks
        protected virtual void Start()
        {
        }

        protected virtual void Update()
        {
            if (!MyOwner) return;

            _doingMelee = (_meleeCoolDownTimer > Time.time);

            if (!MyOwner.ReadActionKeyCode(ActionCodes.Trigger1))
                _singleUsed = false;

            _isScoping = (MyOwner.ReadActionKeyCode(ActionCodes.Trigger2) && _currentlyInUse && !_isReloading && !_doingMelee);

            //visual part of scope (camera fov change and animating weapon position change), only for fpp perspective
            if (MyOwner.IsObserved && _currentlyInUse && _scopePoint)
            {
                if (_changeSensitivityOnScope)
                    MyOwner.SensitivityItemFactorMultiplier = _isScoping ? UserSettings.MouseSensitivityOnSniperScopeMultiplier : 1f;

                MyOwner.IsScoping = _isScoping;

                Transform camera = GameplayCamera._instance.transform;
                Camera _fppCamera = GameplayCamera._instance.FPPCamera;

                _fppCamera.transform.position = Vector3.MoveTowards(_fppCamera.transform.position, (_isScoping ? _scopePoint.position+ _fppCamera.transform.rotation*_aimPositionRecoil : camera.position), ModelLerpSpeed * Time.deltaTime);
                _aimPositionRecoil = Vector3.Lerp(_aimPositionRecoil, Vector3.zero, _recoil_aimCamera_stabilizationSpeed * Time.deltaTime);

                _scopePercantage = Mathf.MoveTowards(_scopePercantage, _isScoping ? 1 : 0, FovLerpSpeed * Time.deltaTime);
                _scopePercantage = Mathf.Clamp(_scopePercantage, 0, 1);

                GameplayCamera._instance.SetFieldOfView(Mathf.Lerp(UserSettings.FieldOfView, ScopeFov, _scopePercantage));

                if (_spawnedUI)
                    _spawnedUI.Scope(_isScoping);
            }
        }

        public void Server_ResetItemValues()
        {
            Server_CurrentAmmo = MagazineCapacity;
            Server_CurrentAmmoSupply = MaxAmmoSupply;

            RpcResetItemValues();
        }
        [ClientRpc]
        void RpcResetItemValues() 
        {
            CurrentAmmo = MagazineCapacity;
            CurrentAmmoSupply = MaxAmmoSupply;
            
            ClientChangeCurrentAmmoCount(CurrentAmmo);
        }

        protected void AddAimRecoild(float recoild) 
        {
            _aimPositionRecoil.z += recoild;
        }

        protected virtual void FixedUpdate()
        {
            //set animation flags in fixed update instead of update for performance benefit
            if (_currentlyInUse && MyOwner && MyOwner.FPP && _myAnimator.runtimeAnimatorController)
                _myAnimator.SetFloat(AnimationNames.ITEM_SPEED, (!MyOwner.Block && MyOwner.IsRunning && !MyOwner.IsCrouching ? 2f: 1f));
        }
        #endregion

        #region melee
        protected virtual void Meele() 
        {
            if (!MyOwner) return;

            MyOwner.CharacterItemManager.StartUsingItem();

            if (isOwned || (MyOwner.BOT && isServer))
            {
                Collider[] col = GetHealthsInMeleeRange();
                for (int i = 0; i < col.Length; i++)
                {
                    Health health = col[i].gameObject.GetComponent<Health>();
                    if (health == null) continue;
                    if (health == MyOwner.Health) continue;

                    if (isOwned)
                        CmdHitMelee(health);
                    else 
                        health.Server_ChangeHealthState(_meleeDamage, 0, AttackType.melee, MyOwner.Health, _meleeAttackForce);
                }
            }

            if (MyOwner.IsObserved) 
                MyOwner.PlayerRecoil.Recoil(2f, -4f, 6, 0.15f);

            MyOwner.CharacterAnimator.ResetTrigger(AnimationNames.ITEM_MELEE);
            MyOwner.CharacterAnimator.SetTrigger(AnimationNames.ITEM_MELEE);
            _myAnimator.ResetTrigger(AnimationNames.ITEM_MELEE);
            _myAnimator.SetTrigger(AnimationNames.ITEM_MELEE);
        }

        [Command]
        void CmdHitMelee(Health health)
        {
            health.Server_ChangeHealthState(_meleeDamage, 0, AttackType.melee, MyOwner.Health, _meleeAttackForce);
        }

        protected Collider[] GetHealthsInMeleeRange() 
        {
            return Physics.OverlapSphere(MyOwner.FPPLook.position + MyOwner.FPPLook.forward * 1f, 1.2f, GameManager.characterLayer);
        }

        [Command]
        void CmdMelee() 
        {
            RpcMeelee();
        }
        [ClientRpc(includeOwner = false)]
        void RpcMeelee() 
        {
           Meele();
        }
        #endregion

        #region inventory managament methods
        /// <summary>
        /// launched when player takes this item to his hands
        /// </summary>
        public virtual void Take()
        {
            _itemActive = false;

            if (_spawnedUI)
                _spawnedUI.gameObject.SetActive(true);

            MyOwner.characterFirePoint.localRotation = Quaternion.identity;

            //setting cooldowns like this will prevent wepapon from firing before taking animation ends
            _currentlyInUse = true;
            _coolDownTimer = float.MaxValue;
            _meleeCoolDownTimer = _coolDownTimer;

            RenderItem(MyOwner.FPP ? ItemRenderType.FppOnly : ItemRenderType.Normal);

            _myAnimator.SetTrigger(AnimationNames.ITEM_TAKE);

            if (takeClip)
                _audioSource.PlayOneShot(takeClip);

            StopTakingItemProcedureIfExist();

            takingItemProcedure = StartCoroutine(TakeItemProcedure());
            IEnumerator TakeItemProcedure() {
                yield return new WaitForSeconds(takingTime);
                _itemActive = true;

                if (MyOwner.FPP)
                    MyOwner.CharacterAnimator.PlayAnimation(AnimationNames.ITEM_IDLE);
                else 
                    MyOwner.CharacterAnimator.PlayAnimation("look");  

                if (_myAnimator.runtimeAnimatorController)
                    _myAnimator.Play(AnimationNames.ITEM_IDLE);

                //setting weapon to be able to fire
                _coolDownTimer = 0;
                _meleeCoolDownTimer = _coolDownTimer;
            }

        }

        //launched if players takes another item, or drops item
        public virtual void PutDown()
        {
            StopTakingItemProcedureIfExist();

            if (_spawnedUI)
                _spawnedUI.gameObject.SetActive(false);

            if (MyOwner.IsObserved)
                GameplayCamera._instance.SetFovToDefault();

            if (MyOwner.FPP) 
            {
                Transform camera = GameplayCamera._instance.transform;
                Camera _fppCamera = GameplayCamera._instance.FPPCamera;
                _fppCamera.transform.position = camera.position;
            }

            _currentlyInUse = false;
            StopAllCoroutines();
            UpdateAmmoInHud("");

            RenderItem(ItemRenderType.DoNotRender);

            _audioSource.Stop();

            _scopePercantage = 0; //cancel any scope progress
        }
        //launched if taking item animation was canceled
        void StopTakingItemProcedureIfExist()
        {
            if (takingItemProcedure != null)
            {
                StopCoroutine(takingItemProcedure);
                takingItemProcedure = null;
            }
        }


        /// <summary>
        /// Set permission to be collectable, turn of if owned by someone
        /// </summary>
        protected void SetInteractable(bool _interactable) //determines if item have physics and can picked or is already aquired by another player
        {
            if (_interactable)
            {
                _modelSticker.SetSticker(null, Vector3.zero);
               // ItemModel.transform.SetParent(transform);
               // ItemModel.transform.SetLocalPositionAndRotation(transform.position, transform.rotation);
            }

            _rg.isKinematic = !_interactable;
            _rg.useGravity = _interactable;
            _interactionCollider.enabled = _interactable;
            _collider.enabled = _interactable;
        }

        public virtual bool CanBeUsed()
        {
            return _itemActive && _currentlyInUse;
        }
        public virtual bool CanBeEquiped() { return true; }
        #endregion

        #region item usage
        public virtual void PushLeftTrigger()
        {
            if (!MyOwner) return;

            if (_coolDownTimer <= Time.time && MyOwner.IsAbleToUseItem && PrimaryFireAvailable())
            {
                if (Firemode == FireMode.Single && _singleUsed) return;

                _singleUsed = true;
                _coolDownTimer = Time.time + coolDown;
                Use();
            }
        }
        public virtual void PushRightTrigger()
        {
            if (!MyOwner) return;

            if (CooldownSecondary() && SecondaryFireAvailable())
                SecondaryUse();
        }
        protected virtual bool PrimaryFireAvailable() { return true; }
        protected virtual bool SecondaryFireAvailable() { return true; }

        public virtual void PushReload()
        {
           
        }
        public void PushMeele() 
        {
            if (_currentlyInUse && !_isReloading && _meleeCoolDownTimer <= Time.time)
            {
                _meleeCoolDownTimer = Time.time+_meleeCooldown;

                if(!MyOwner.BOT)
                    Meele();

                if (isServer)
                    RpcMeelee();
                else
                    CmdMelee();
            }
        }

        /// <summary>
        /// Here put the code that only client who uses item or server can run, like giving damage to others
        /// This method already accounts for cool down, and launches synchronized events "SingleUse"
        /// If You don't want this functionality override methods "PushLeftTrigger" or "PushRigthTrigger"
        /// They are called each frame client pushes Inputs for firing (or bot ai calls them it this is bot)
        /// </summary>
        protected virtual void Use()
        {
            if (!MyOwner) return;

            if (isOwned)
            {
                SingleUse(); //for client to for example immediately see muzzleflash when he fires his gun
                CmdSingleUse();
            }
            else if (isServer)
            {
                SingleUse();
                RpcSingleUse();
            }
        }
        //alternate fire mode for method above
        protected virtual void SecondaryUse()
        {
            if (isOwned) 
            {
                SingleSecondaryUse(); //for client to for example immediately see muzzleflash when he fires his gun
                CmdSingleSecondaryUse();
            }else if (isServer)
            {
                SingleSecondaryUse();
                RpcSingleSecondaryUse();
            }
        }

        protected virtual bool CooldownSecondary()
        {
            return false;
        }
        #endregion

        #region use callbacks
        [Command]
        protected virtual void CmdSingleUse()
        {
            RpcSingleUse();
        }
        [ClientRpc(includeOwner = false)]
        protected virtual void RpcSingleUse()
        {
            if (MyOwner)
                SingleUse();
        }
        /// <summary>
        /// Here put the code that every client should run, like playing animations, visual part of recoil, etc.
        /// </summary>
        protected virtual void SingleUse()
        {

            if (MyOwner.PlayerRecoil)
                MyOwner.PlayerRecoil.Recoil(_recoil, _devation, _speed, _duration);

            if (MyOwner.ToolMotion)
                MyOwner.ToolMotion.Shoot(_modelRecoil);

            CurrentRecoil += _recoil_angleAddedOnShot;
            CurrentRecoil = Mathf.Clamp(CurrentRecoil, _recoil_minAngle, _recoil_maxAngle);
        }
        #endregion

        #region secondary use callbacks
        [Command]
        protected virtual void CmdSingleSecondaryUse()
        {
            RpcSingleSecondaryUse();
        }
        [ClientRpc(includeOwner = false)]
        protected virtual void RpcSingleSecondaryUse()
        {
            SingleSecondaryUse();
        }
        /// <summary>
        /// Here put the code that every client should run, like playing animations etc.
        /// </summary>
        protected virtual void SingleSecondaryUse()
        {
            if (!MyOwner)
                return;

            if (MyOwner.PlayerRecoil)
                MyOwner.PlayerRecoil.Recoil(_recoil, _devation, _speed, _duration);

            if (MyOwner.ToolMotion)
                MyOwner.ToolMotion.Shoot(_modelRecoil);

            CurrentRecoil += _recoil_angleAddedOnShot;
        }
        #endregion

        #region pickup and drop
        /// <summary>
        /// Function launched when item is picked up by character
        /// </summary>
        public virtual void AssignToCharacter(CharacterInstance _owner)
        {
            MyOwner = _owner;

            SetPerspective(_owner.FPP);

            SetInteractable(false);

            if (isServer && DestroyCoroutine != null)
            {
                StopCoroutine(DestroyCoroutine);
                DestroyCoroutine = null;
            }

            if (_networkTransform)
                _networkTransform.enabled = false;

        }

        

        /// <summary>
        /// Function launched when item is dropped
        /// </summary>
        public virtual void Drop()
        {
            if(_currentlyInUse)
                PutDown();

            DespawnItemUIifExist();

            RenderItem(ItemRenderType.Normal); //everyone can see dropped item

            if(_myAnimator.runtimeAnimatorController) _myAnimator.Play(AnimationNames.ITEM_IDLE);

            SetInteractable(true);
            MyOwner = null;

            //initialize self destruct coroutine to prevent excessive number of items in game world
            if (isServer)
            {
                DestroyCoroutine = StartCoroutine(CountToDestroy());
            }

            if (_networkTransform)
                _networkTransform.enabled = true;
        }


        public bool CanBePickedUpBy(CharacterInstance character) 
        {
            if (TeamOwnership == -1) return true;

            return (character.Health.Team == TeamOwnership);
        }
        #endregion

        #region dealing damage
        /// <summary>
        /// client request to damage someone that he hitted
        /// </summary>
        [Command]
        protected void CmdDamage(byte damagedHealthID, CharacterPart hittedPart, float damagePercentage, AttackType attackType)
        {
            Health victim = GameSync.Singleton.Healths.GetObj(damagedHealthID);
            if (victim == null) return;

            if (Server_CurrentAmmo > 0)
                ServerDamage(victim, hittedPart, damagePercentage, attackType);
        }

        /// <summary>
        /// apply damage to victim
        /// </summary>
        protected void ServerDamage(Health damagedHealth, CharacterPart hittedPart, float damagePercentage, AttackType attackType)
        {
            damagePercentage = Mathf.Clamp(damagePercentage, 0, 1);
            damagedHealth.Server_ChangeHealthState(Mathf.FloorToInt(_damage * damagePercentage),
                hittedPart, attackType, MyOwner.Health, AttackForce);
        }
        #endregion

        #region Client side methods (animations, rendering, UI)
        //set rendering, animations and audio appropriately for player perspective
        public void SetPerspective(bool fpp)
        {
            if (_lowPolyMesh) 
            {
                _itemMesh.sharedMesh = fpp ? _highPolyMesh: _lowPolyMesh;
                _itemMesh.updateWhenOffscreen = fpp;
                _myAnimator.cullingMode = fpp ? AnimatorCullingMode.AlwaysAnimate : AnimatorCullingMode.CullCompletely;
            }

            DespawnItemUIifExist();

            //if weapon is udes in first person perspective, then we want it to play sound in both speakers, if its not,
            //play sound in speakers according to 3D space
            _audioSource.spatialBlend = fpp ? 0 : 1f;

            //set other audiosource values to work in 3D space
            _audioSource.spread = 180;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
            _audioSource.maxDistance = 50;
            _audioSource.minDistance = 10;

            _myAnimator.runtimeAnimatorController = fpp ? AnimatorControllerForItemFPP : AnimatorControllerForItem;

            if (!_placeInHandInFPP || fpp == false)
            {
                _modelSticker.SetSticker(MyOwner.CharacterItemManager.ItemTarget, MyOwner.CharacterItemManager.ItemRotationCorrector);
            }
            else
            {
                _modelSticker.SetSticker(MyOwner.CharacterItemManager.ItemTargetFPP, MyOwner.CharacterItemManager.ItemFPPRotationCorrector);
            }


            if (fpp && _itemUI)
            {
                _spawnedUI = Instantiate(_itemUI).GetComponent<HUDItem>();
                _spawnedUI.gameObject.SetActive(_currentlyInUse);
            }

            RenderItem(_currentlyInUse ? (fpp ? ItemRenderType.FppOnly : ItemRenderType.Normal) : ItemRenderType.DoNotRender);
        }

        void DespawnItemUIifExist()
        {
            if (_spawnedUI)
                Destroy(_spawnedUI.gameObject);
        }

        protected void UpdateAmmoInHud(string ammo, string supply = null)
        {
            //print("Ammo: {ammo}")
            if (MyOwner && MyOwner.IsObserved && _currentlyInUse)
            {
                UICharacter._instance.OnAmmoStateChanged(ammo, supply);
            }
        }

        void RenderItem(ItemRenderType type)
        {
            switch (type)
            {
                case ItemRenderType.Normal:
                    ItemModel.SetActive(true);
                    GameTools.SetLayerRecursively(ItemModel, 0);
                    _myAnimator.runtimeAnimatorController = AnimatorControllerForItem;

                    if (_lowPolyMesh)
                    {
                        _itemMesh.sharedMesh = _lowPolyMesh;
                    }
                    break;
                case ItemRenderType.FppOnly:
                    ItemModel.SetActive(true);
                    GameTools.SetLayerRecursively(ItemModel, 13);

                    if (_lowPolyMesh)
                    {
                        _itemMesh.sharedMesh = _highPolyMesh;
                    }
                    break;
                case ItemRenderType.DoNotRender:
                    ItemModel.SetActive(false);
                    break;
            }
        }

        public void ServerSetSkin(int[] selectedSkins)
        {
            if (selectedSkins == null) return;

            if (selectedSkins.Length == 0) return;

            ItemSkinContainer[] skins = ClientInterfaceManager.Instance.ItemSkinContainers;

            for (int i = 0; i < skins.Length; i++)
            {
                if (ItemName != skins[i].ItemName) continue;

                if (selectedSkins[i] == -1) return; //its default skin, do not change anything

                RpcSetSkin(selectedSkins[i]);
            }
        }
        [ClientRpc]
        void RpcSetSkin(int selectedSkinID)
        {
            ItemSkinContainer[] skins = ClientInterfaceManager.Instance.ItemSkinContainers;

            for (int i = 0; i < skins.Length; i++)
            {
                if (ItemName != skins[i].ItemName) continue;

                _itemMesh.material = skins[i].Skins[selectedSkinID].Skin;
            }
        }

        

        #endregion

        #region ammo managament
        /// <summary>
        /// Use this to add ammo to ammo supply for item. Returns taken amount from what was given, may be all of it, may be just a part of it
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public int AddAmmo(int amount) 
        {
            if (Server_CurrentAmmoSupply >= MaxAmmoSupply) return 0;

            int neededAmmo = MaxAmmoSupply - Server_CurrentAmmoSupply;
            if (amount > neededAmmo)
            {
                Server_CurrentAmmoSupply += neededAmmo;
                amount = neededAmmo;
            }
            else 
            {
                Server_CurrentAmmoSupply += amount;
            }

            RpcReceiveAmmo(Server_CurrentAmmoSupply, amount);
            return amount;
        }

        protected void ClientChangeCurrentAmmoCount(int currentAmmo)
        {
            CurrentAmmo = currentAmmo;
            OnCurrentAmmoChanged();
        }
        protected virtual void OnCurrentAmmoChanged() 
        {

        }

        [ClientRpc]
        void RpcReceiveAmmo(int finalAmount, int amountTaken) 
        {
            CurrentAmmoSupply = finalAmount;
            UpdateAmmoInHud(CurrentAmmo.ToString(), CurrentAmmoSupply.ToString());
            MyOwner.Client_OnPickedupObject?.Invoke($"{ItemName} ammo / x{amountTaken}");
        }
        #endregion

        #region server management
        /// <summary>
        /// If item is dropped and lonely for too long then we want to destroy it
        /// </summary>
        IEnumerator CountToDestroy()
        {
            yield return new WaitForSeconds(GameManager.TimeOfLivingLonelyItem);

            if (!MyOwner)
                NetworkServer.Destroy(gameObject);
        }
        #endregion

        public enum FireMode
        {
            Automatic,
            Single,
        }
    }

    public enum ItemRenderType
    {
        Normal, //for everyone
        FppOnly, //only for fpp perspective
        DoNotRender, //do not render, if item is possed by player but not in use
    }

    public enum GameItemType : int
    {
        Normal,
        Pocket,
    }
}
