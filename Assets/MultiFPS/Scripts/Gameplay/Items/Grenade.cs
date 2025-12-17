using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiFPS.Gameplay
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MultiFPS/Items/Grenade")]
    public class Grenade : Item
    {
        public GameObject ThrowablePrefab;
        public float RigidBodyForce = 2500;

        protected override void Awake()
        {
            base.Awake();

            ClientChangeCurrentAmmoCount(CurrentAmmoSupply);
        }
        protected override void Use()
        {
            base.Use();

            if (CurrentAmmoSupply <= 0) return;

            if (isServer)
                SpawnThrowable(new Vector2(MyOwner.Input.LookX, MyOwner.Input.LookY));
            else
            {
                CmdSpawnThrowable(new Vector2(MyOwner.Input.LookX, MyOwner.Input.LookY));

                CurrentAmmoSupply--;
                ClientChangeCurrentAmmoCount(CurrentAmmoSupply);
            }

            if (CurrentAmmoSupply <= 0) MyOwner.CharacterItemManager.ChangeItemDelay(-1, 0.45f);
        }
        [Command]
        void CmdSpawnThrowable(Vector2 look) 
        {
            SpawnThrowable(look);
        }
        void SpawnThrowable(Vector2 look) 
        {
            if (CurrentAmmoSupply <= 0) return;

            CurrentAmmoSupply--;
            ClientChangeCurrentAmmoCount(CurrentAmmoSupply);

            GameObject throwable = Instantiate(ThrowablePrefab, MyOwner.Health.GetPositionToAttack(), Quaternion.Euler(MyOwner.Input.LookX, MyOwner.Input.LookY, 0));

            Vector3 force = Quaternion.Euler(look.x, look.y, 0)*Vector3.forward * RigidBodyForce;

            throwable.GetComponent<Throwable>().Activate(MyOwner, force);
            NetworkServer.Spawn(throwable);
        }
        public override void Take()
        {
            base.Take();
            UpdateAmmoInHud(CurrentAmmo.ToString());
        }

        /*protected override void OnOwnerPickedupAmmo()
        {
            //UpdateAmmoInHud(CurrentAmmoSupply.ToString());
        }*/

        protected override void SingleUse()
        {
            //if we are out of granades, hide granade model from player hand
            

            MyOwner.CharacterItemManager.StartUsingItem(); //will disable ability tu run for 0.5 seconds
            MyOwner.CharacterAnimator.SetTrigger("recoil");
            _myAnimator.SetTrigger("recoil");
        }

        public override bool CanBeEquiped()
        {
            return base.CanBeEquiped() && CurrentAmmoSupply > 0;
        }

        protected override void OnCurrentAmmoChanged()
        {
            UpdateAmmoInHud(CurrentAmmo.ToString());
        }
    }
}