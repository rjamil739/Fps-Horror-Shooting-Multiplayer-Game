using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace MultiFPS.Gameplay
{
    public class CharacterItemManager : DNNetworkBehaviour
    {
        public List<Slot> Slots = new List<Slot>();

        public Item CurrentlyUsedItem { private set; get; }
        public int CurrentlyUsedSlotID { private set; get; } = -1;

        //for killfeed when we kill someone after death
        public int PreviouslyUsedSlotID { private set; get; } = -1;

        /// <summary>
        /// max item slots for character
        /// </summary>
        CharacterInstance _characterInstance;

        Transform _fppCameraTarget;

        //for killfeed to know what item to display
        [HideInInspector] public Item LastUsedItem { private set; get; }

        Coroutine _c_usingItem;

        /// <summary>
        /// hand to place item in
        /// </summary>
        [SerializeField] Transform _itemPointInHand;

        /// <summary>
        /// camera point to place item in in case of character using this component being rendered in FPP mode
        /// </summary>
        [SerializeField] Transform _itemTargetFPP;
        public Transform ItemTargetFPP;

        /// <summary>
        /// Final item point that will be one of those two above, depending on character perspective
        /// </summary>
        [HideInInspector] public Transform ItemTarget;

        /// <summary>
        /// items can stick to character model in wrong direction, so adjusting this varable will correct that
        /// </summary>
        [SerializeField] private Vector3 _itemRotationCorrector; //item rotation corrector for player 3rd person models, for fpp we will use Vector.Zero
        [HideInInspector] public Vector3 ItemRotationCorrector; //what will be actually used
        public Vector3 ItemFPPRotationCorrector; //what will be actually used

        public delegate void CharacterEvent_EquipmentChanged(int currentlyUsedSlot);
        public CharacterEvent_EquipmentChanged Client_EquipmentChanged { get; set; }

        /*tracks how much time has passed since player stopped running
         useful for not letting player shoot right after he stopped running*/
        float _walkingTimer; 
        float _itemUsageTimer; //tracks how much time has passed since player used his item for the last time

        Item[] _rememberedItems;

        private void Awake()
        {
            _rememberedItems = new Item[Slots.Count];
        }

        private void Start()
        {
            _fppCameraTarget = _characterInstance.FPPCameraTarget;
            _walkingTimer = 1f; //let player use his items right from the start
        }

        private void Update()
        {
            if (_characterInstance.IsRunning)
                _walkingTimer = 0;
            else _walkingTimer += Time.deltaTime;

            _itemUsageTimer += Time.deltaTime;

            _characterInstance.IsAbleToUseItem = _walkingTimer > 0.125f;
            _characterInstance.IsUsingItem = _itemUsageTimer < 0.3f;
        }

        public void Setup()
        {
            _characterInstance = GetComponent<CharacterInstance>();
            _characterInstance.Health.Server_OnHealthDepleted += OnDeath;
            _characterInstance.Client_OnPerspectiveSet += OnPerspectiveSet;

            ItemTarget = _itemPointInHand;
        }

        private void OnDestroy()
        {
            _characterInstance.Health.Server_OnHealthDepleted -= OnDeath;
            _characterInstance.Client_OnPerspectiveSet -= OnPerspectiveSet;

            for (int i = 0; i < Slots.Count; i++)
            {
                Slots[i].Item = null;
                Slots[i] = null;
            }

            _characterInstance = null;

            _fppCameraTarget = null;

            CurrentlyUsedItem = null;
            LastUsedItem = null;

            _itemPointInHand = null;
            _fppCameraTarget = null;
            _itemTargetFPP = null;
            ItemTarget = null;
        }

        #region spawn
        //Spawn and attach to characters items selected in inspector
        public void SpawnStarterEquipment()
        {
            //prepare items for attaching to player
            for (int i = 0; i < Slots.Count; i++)
            {
                Slot slot = Slots[i];

                if (ReassingItemsFromPreviousLife)
                {
                    Item rememberedItem = _rememberedItems[i];
                    if (rememberedItem != null && !rememberedItem.MyOwner) //reuse item if possible
                    {
                        rememberedItem.Server_ResetItemValues();
                        continue;
                    }
                }

                //if we cannot reassign item, create new one
                GameObject itemPrefab = slot.ItemOnSpawn;
                if (itemPrefab)
                {
                    Item itemGM = Instantiate(itemPrefab, transform.position, transform.rotation).GetComponent<Item>();
                    NetworkServer.Spawn(itemGM.gameObject);
                    _rememberedItems[i] = itemGM;
                }
            }

            //attach items to player
            for (int i = 0; i < _rememberedItems.Length; i++)
            {
                if (_rememberedItems[i] == null)
                    continue;

                AttachItemToCharacter(_rememberedItems[i], i);
            }
        }
        #endregion

        #region player/bot inputs
        /// <summary>
        /// Fire inputs, launched by clients and bots to use items
        /// </summary>
        public void Fire1()
        {
            //launching this will tell character that if it runs then it must stop running in order to be able to use item
            //and this will also make character unable to run for 0.25s
            StartUsingItem(); 

            //dont execute this if its not client or bot, other clients will see item being differently
            if (!_characterInstance.IsClientOrBot()) return;
            if (isServer && connectionToClient != null && !isOwned) return;

            //dont pull trigger if dead
            if (CurrentlyUsedItem && !_characterInstance.Block && _characterInstance.Health.CurrentHealth > 0)
                CurrentlyUsedItem.PushLeftTrigger();
        }
        public void Fire2()
        {
            StartUsingItem();

            if (!_characterInstance.IsClientOrBot()) return;
            if (isServer && connectionToClient != null && !isOwned) return;

            if (!_characterInstance.Block && _characterInstance.Health.CurrentHealth > 0 && CurrentlyUsedItem)
                CurrentlyUsedItem.PushRightTrigger();
        }
        public void Reload() 
        {
            //dont push reload trigger if dead
            if (CurrentlyUsedItem && !_characterInstance.Block && _characterInstance.Health.CurrentHealth > 0)
                CurrentlyUsedItem.PushReload();
        }

        //is is always called by client to:
        public void ClientTakeItem(int _slotID)
        {
            Take(_slotID);//1 take item int this slot and see action immediately locally
            CmdTakeItem(_slotID);//2 send messade to server that we took item in this slot
        }

        public void TakePreviousItem() 
        {
            if (PreviouslyUsedSlotID == CurrentlyUsedSlotID) return;

            if (_characterInstance.BOT && isServer)
            {
                Take(PreviouslyUsedSlotID);
                RpcTakeItem(PreviouslyUsedSlotID);
            }
            else if (isOwned)
                ClientTakeItem(PreviouslyUsedSlotID);
        }
        #endregion


        #region item managament
        public void StartUsingItem()
        {
            _itemUsageTimer = 0;
        }
        /// <summary>
        /// launched when player takes this item to his hands
        /// </summary>
        void Take(int slotID)
        {
            slotID = Mathf.Clamp(slotID, 0, Slots.Count - 1);

            if (CurrentlyUsedItem == Slots[slotID].Item && Slots[slotID].Item)
                return; //dont retake current item

            if (Slots[slotID].Item && !Slots[slotID].Item.CanBeEquiped())
                return;

            //pocket slots are extra, so if no item is assigned to them then dont let player take them
            if (!Slots[slotID].Item && Slots[slotID].Type == SlotType.PocketItem && CurrentlyUsedSlotID != slotID)
                return;

            _characterInstance.SensitivityItemFactorMultiplier = 1f;
            _characterInstance.IsReloading = false;

            if (CurrentlyUsedItem)
                PutDownItem(CurrentlyUsedItem);

            //if we retake current slot, dont make it last used slot
            if(CurrentlyUsedSlotID != slotID)
                PreviouslyUsedSlotID = CurrentlyUsedSlotID;

            CurrentlyUsedSlotID = slotID;
            CurrentlyUsedItem = Slots[slotID].Item;

            if (CurrentlyUsedItem)
                LastUsedItem = CurrentlyUsedItem;

            Client_EquipmentChanged?.Invoke(CurrentlyUsedSlotID);

            if (CurrentlyUsedItem)
                CurrentlyUsedItem.Take();
        }

        /// <summary>
        /// Boolean for checking if we already own a weapon that we want to pick up
        /// for example: dont allow player to pick up m4 when he already owns m4
        /// </summary>
        public bool AlreadyAquired(Item _item)
        {
            foreach (Slot i in Slots)
                if (i.Item && i.Item.ItemName == _item.ItemName)
                    return true;
            return false;
        }

        /// <summary>
        /// If player changes item then send this info to server so everyone else will se that change
        /// </summary>
        [Command]
        void CmdTakeItem(int _slotID)
        {
            if(!isOwned)
                Take(_slotID);

            RpcClientTookItem(_slotID);
        }
        /// <summary>
        /// bunch of checks launched on client side when he want to grab weapon
        /// 
        /// check if player looks at weapoon, then if he does check if we dont have
        /// already this weapon in eq
        /// </summary>     
        public void TryGrabItem()
        {
            //don't fill slots that are not meant to be filled
            //if (Slots[CurrentlyUsedSlotID].Type == SlotType.BuiltIn) return;

            RaycastHit hit;
            if (Physics.Raycast(_fppCameraTarget.position, _fppCameraTarget.forward, out hit, 3.5f, GameManager.interactLayerMask))
            {
                GameObject go = hit.collider.gameObject;
                Item _item = go.GetComponent<Item>();

                if (_item)
                    if (!AlreadyAquired(_item))
                        CmdPickUpItem(_item.netIdentity, CurrentlyUsedSlotID);
            }
        }

        //launched by client input to drop item
        public void TryDropItem()
        {
            CmdDropItem(CurrentlyUsedSlotID);
        }

        /// <summary>
        /// Switches to desired item after given delay in seconds. Can be launched only for clients who own character object, or on server if
        /// it is bot. Dont run this from server for client
        /// </summary>
        public void ChangeItemDelay(int slotID = -1, float delay = 0f) 
        {
            StartCoroutine(ChangeItemDelayProcedure());
            IEnumerator ChangeItemDelayProcedure()
            {
                yield return new WaitForSeconds(delay);
                if (slotID == CurrentlyUsedSlotID)
                    yield break;

                if (slotID == -1)
                {
                    TakePreviousItem();
                    yield break;
                }

                if (isOwned)
                    ClientTakeItem(slotID);
                else
                {
                    RpcTakeItem(slotID);
                    Take(slotID);
                }
            }
        }

        /// <summary>
        /// server processor for client request to pickup item
        /// </summary>
        [Command]
        void CmdPickUpItem(NetworkIdentity _itemNetIdentity, int _slotID)
        {
            //do not let dead character pickup weapons
            if (_characterInstance.Health.CurrentHealth <= 0) return;

            AttachItemToCharacter(_itemNetIdentity.GetComponent<Item>(), _slotID);
        }
        public void AttachItemToCharacter(Item _item, int _slotID)
        {
            if (!_item.CanBePickedUpBy(_characterInstance)) return;

            if (AlreadyAquired(_item))
            {
                print($"MultiFPS: Gameplay: This item is already aquired by {name}: {_item.name}");
                return; //if player tries to equip same item twice than dont allow that
            }

            //prefer to add item to current slot, if its empty
            if (!Slots[_slotID].Item && Slots[_slotID].Type == _item.SlotType) 
            {
                AttachItemToSlot(_slotID);
                return;
            }

            //searching for free slot when current slot is not available
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].Type == _item.SlotType && !Slots[i].Item)
                {
                    AttachItemToSlot(i);
                    return;
                }
            }

            //replace item in currently used slot when eq if full, but only if item type matches currently used slot type
            int numberOfCheckedSlots = 0;
            for (int i = _slotID; i < Slots.Count+1; i++)
            {
                if (i >= Slots.Count)
                    i = 0;

                numberOfCheckedSlots++;
                if (numberOfCheckedSlots > Slots.Count) return; //cannot replace item, didnt find suitable slot for it

                if (Slots[i].Type != _item.SlotType) continue;

                Take(i);
                //for clients
                RpcTakeItem(i);
                Server_DropItem(i);

                AttachItemToSlot(i);
                break;
            }

            void AttachItemToSlot(int __slotID)
            {
                //for server
                AssignItem(__slotID, _item);
                RpcAssignItem(__slotID, _item);

                Take(__slotID);
                //for clients
                RpcTakeItem(__slotID);
            }
        }
        [ClientRpc]
        void RpcAssignItem(int slotID, Item itemToAssign)
        {
            if (isServer) return;

            AssignItem(slotID, itemToAssign);
        }

        /// <summary>
        /// assign item to character equipment
        /// </summary>
        void AssignItem(int slotID, Item item)
        {
            if (!item)
            {
                Debug.LogWarning("Received message to attach item to character, but item with given netID does not exist");
                return;
            }

            if (isServer && netIdentity.connectionToClient != null) 
                item.netIdentity.AssignClientAuthority(netIdentity.connectionToClient);


            Slots[slotID].Item = item;
            item.AssignToCharacter(_characterInstance);

            PutDownItem(item);
        }


        [ClientRpc]
        void RpcTakeItem(int slotID)
        {
            if (!isServer)
                Take(slotID);
        }
        [ClientRpc(includeOwner = false)]
        void RpcClientTookItem(int slotID)
        {
            if (isServer) return;

            Take(slotID);
        }

        public void ServerCommandTakeItem(int slotID) 
        {
            Take(slotID);
            RpcClientTakeItemIncludeOwner(slotID);
        }
        [ClientRpc]
        void RpcClientTakeItemIncludeOwner(int slotID)
        {
            if (isServer) return;
                Take(slotID);
        }
        //drop
        [Command]
        void CmdDropItem(int slotIDtoDrop)
        {
            Item itemToDrop = Slots[slotIDtoDrop].Item;

            if (!itemToDrop) return;
            if (!itemToDrop.CanBeDropped) return;

            Server_DropItem(slotIDtoDrop);
        }
        public void Server_DropItem(int slotIDtoDrop = -1)  
        {
            if (slotIDtoDrop == -1) slotIDtoDrop = CurrentlyUsedSlotID;

            Drop(slotIDtoDrop);
            RpcDropItem(slotIDtoDrop);
        }
        [ClientRpc]
        void RpcDropItem(int slotIDtoDrop)
        {
            if (!isServer) Drop(slotIDtoDrop);
        }

        /// <summary>
        /// detach item from character and drop it
        /// </summary>
        void Drop(int slotIDtoDrop)
        {
            Slot slotToEmpty = Slots[slotIDtoDrop];

            if (slotToEmpty.Item)
            {
                Item itemToDrop = slotToEmpty.Item;
                if (itemToDrop)
                {
                    bool thisItemIsCurrentlyInUse = slotIDtoDrop == CurrentlyUsedSlotID;

                    slotToEmpty.Item = null;

                    if (isServer)
                        itemToDrop.netIdentity.RemoveClientAuthority();

                    itemToDrop.Drop();

                    if (isServer)
                    {
                        if (thisItemIsCurrentlyInUse)
                        {
                            itemToDrop.transform.position = _fppCameraTarget.position + Quaternion.Euler(_characterInstance.Input.LookX, _characterInstance.Input.LookY, 0) * new Vector3(0,-1,1) * 0.2f;
                            itemToDrop.GetComponent<Rigidbody>().AddForce(Quaternion.Euler(_characterInstance.Input.LookX, _characterInstance.Input.LookY, 0) * Vector3.forward * 350f); //push weapon forward on drop
                        }
                        else
                        {
                            Quaternion newItemRotation = Quaternion.Euler(Random.Range(0, 360), Random.Range(-90, 90), 0);
                            Vector3 newItemPosition = _fppCameraTarget.position + transform.rotation * new Vector3(0, -1, 0.5f) * (0.2f + 0.15f * slotIDtoDrop);
                            newItemPosition += transform.rotation*Vector3.right * Random.Range(-0.4f, 0.4f);

                            itemToDrop.transform.position = newItemPosition;
                            itemToDrop.transform.rotation = newItemRotation;

                            itemToDrop.GetComponent<Rigidbody>().AddForce(newItemRotation * Vector3.forward * 85f); //push weapon forward on drop
                        }
                    }
                    
                    CurrentlyUsedItem = null;
                    Take(slotIDtoDrop);
                }
            }
        }

        /// <summary>
        /// when changing item, put down old item before taking new one
        /// </summary>
        void PutDownItem(Item _itemToPutdown)
        {
            if (_itemToPutdown)
                _itemToPutdown.PutDown();
        }
        #endregion

        #region character events

        /// <summary>
        /// if we want to destroy character, destroy also character equipment
        /// </summary>
        public void OnDespawnCharacter()
        {
            DespawnOwnedItems();
        }

        public void DespawnOwnedItems()
        {
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].Item)
                    NetworkServer.Destroy(Slots[i].Item.gameObject);
            }

            for (int i = 0; i < _rememberedItems.Length; i++)
            {
                Item rememberedItem = _rememberedItems[i];
                if (!rememberedItem) continue;

                if (rememberedItem.MyOwner == null)
                {
                    NetworkServer.Destroy(rememberedItem.gameObject);

                }
            }
        }

        public void OnPerspectiveSet(bool fpp)
        {
            ItemTarget = fpp ? _itemTargetFPP : _itemPointInHand;
            ItemRotationCorrector = fpp ? Vector3.zero : _itemRotationCorrector;

            for (int i = 0; i < Slots.Count; i++)
            {
                Item item = Slots[i].Item;
                if (item)
                    item.SetPerspective(fpp);
            }
        }

        public bool ReassingItemsFromPreviousLife = true;

        /// <summary>
        /// if dead drop all items
        /// </summary>
        public void OnDeath(CharacterPart damagedPart, AttackType attackType, Health attacker, int attackForce)
        {
            CurrentlyUsedSlotID = -1;
            PreviouslyUsedSlotID = -1;
            CurrentlyUsedItem = null;

            if(ReassingItemsFromPreviousLife)
                for (int i = 0; i < Slots.Count; i++)
                    _rememberedItems[i] = Slots[i].Item;

            for (int i = 0; i < Slots.Count; i++)
            {
                Server_DropItem(i);
            }
        }
        #endregion

        #region UpdateLatePlayer

        /// <summary>
        /// if new player joins the game, and others are already spawned on the map, then his game should know equipment of those other players
        /// who were already playing on the server, and these methods do exactly that, they will tell new client wchich items do those players have
        /// </summary>
        protected override void OnNewPlayerConnected(NetworkConnection conn)
        {
            List<NetworkIdentity> itemIdenties = new List<NetworkIdentity>();
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].Item)
                {
                    itemIdenties.Add(Slots[i].Item.GetComponent<NetworkIdentity>());
                }
                else
                {
                    itemIdenties.Add(null);
                }
            }
            TargetRpcUpdateForLatePlayer(conn, itemIdenties, CurrentlyUsedSlotID);
        }
        [TargetRpc]
        void TargetRpcUpdateForLatePlayer(NetworkConnection conn, List<NetworkIdentity> itemsID, int currentlyUsedSlot)
        {
            for (int i = 0; i < itemsID.Count; i++)
            {
                if (itemsID[i])
                {
                    AssignItem(i, itemsID[i].GetComponent<Item>());
                }
            }
            Take(currentlyUsedSlot);
        }
        #endregion


        #region Unity editor
        //unity editor only, inspector value validation
        protected override void OnValidate()
        {
            base.OnValidate();

            for (int i = 0; i < Slots.Count; i++) 
            {
                if (Slots[i].ItemOnSpawn)
                {
                    Item itemCheck = Slots[i].ItemOnSpawn.GetComponent<Item>();

                    if (!itemCheck)
                    {
                        Debug.LogError("MultiFPS WARNING: Item that is meant to be used by player must have Item component attached to it!");
                        Slots[i].ItemOnSpawn = null;
                    }
                }
            }
        }
        #endregion

    }
    [System.Serializable]
    public class Slot 
    {
        //slot type, determines if item can be dropped or replaced by another, or not
        //Normal: item can be dropped/replaced by another
        //BuildIn: item will stay in this slot forever
        //You can customize slots hovever you like in the inspector
        public SlotType Type;
        /// <summary>
        /// what input players has to press to select this slot
        /// </summary>
        public SlotInput SlotInput;
        //actual gameplay item, dont drag anything here in the inspector, game will fill that on runtime.
        //it does not need to be visible in the inspector, but we kept it visible so You can see what is going on real time
        //You can hide it if you like by uncommenting "[HideInInspector]" attribute below

        /*[HideInInspector]*/
        public Item Item;

        //If you wish player character to have certain default item for certain slot
        //then drag and drop here that item prefab from project files
        public GameObject ItemOnSpawn;

        public string SpecificItemOnly;
    }
    public enum SlotType
    {
        Normal, //=> item can be dropped/replaced by another
        //BuiltIn, //=>  item will stay in this slot forever
        PocketItem,
    }
    public enum SlotInput 
    {
        I_1,
        I_2,
        I_3,
        I_4,
        I_X,
        I_Z,
    }
}
