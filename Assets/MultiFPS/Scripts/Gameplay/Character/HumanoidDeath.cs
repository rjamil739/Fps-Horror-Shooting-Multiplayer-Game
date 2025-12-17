using System;
using UnityEngine;

namespace MultiFPS.Gameplay
{
    [DisallowMultipleComponent]
    /// <summary>
    /// Component responsible for spawning ragdoll on character death
    /// </summary>
    public class HumanoidDeath : MonoBehaviour
    {
        [Tooltip("Ragdoll prefab that will be spawned on character death")]
        [SerializeField] GameObject ragDoll_Prefab;
        [SerializeField] Transform _head;
        
        [Tooltip("Clip that will be player when character receives headshot")]
        [SerializeField] AudioClip _impactClip_head;
        [SerializeField] AudioClip _impactClip_body;

        CharacterInstance _characterInstance;
        AudioSource _audioSource;
        RagDoll _spawnedRagdoll;
        Health _health;

        [SerializeField] GameObject _headImpactEffectPrefab;
        ObjectPool _headImpactEffectPool;

        [SerializeField] GameObject _headBloodEffectPrefab;
        ObjectPool _headBloodEffectPool;

        private void Awake()
        {
            _characterInstance = GetComponent<CharacterInstance>();
            _audioSource = GetComponent<AudioSource>();
            _health = GetComponent<Health>();

            _headImpactEffectPool = ObjectPooler.Instance.GetPoolByName(_headImpactEffectPrefab.name);
            _headBloodEffectPool = ObjectPooler.Instance.GetPoolByName(_headBloodEffectPrefab.name);  
        }
        private void Start()
        {

            _health.Server_OnHealthDepleted += ServerHealthDepleted;
            _health.Client_OnHealthStateChanged += CheckHealthState;

            _health.Client_Resurrect += Client_OnResurrected;
            _health.Server_Resurrect += Server_OnResurrected;

            _health.Client_OnHealthDepleted += ClientOnHealthDepleted;
        }

        private void OnDestroy()
        {
            _health.Server_OnHealthDepleted -= ServerHealthDepleted;
            _health.Client_OnHealthStateChanged -= CheckHealthState;

            _health.Client_Resurrect -= Client_OnResurrected;
            _health.Server_Resurrect -= Server_OnResurrected;
            _health.Client_OnHealthDepleted -= ClientOnHealthDepleted;
        }

        void Client_OnResurrected(int health) 
        {
            _characterInstance.CharacterAnimator.RenderCharacterModel(true);
            

            if (_spawnedRagdoll)
                Destroy(_spawnedRagdoll.gameObject);
        }
        void Server_OnResurrected(int health) 
        {
            _characterInstance.CharacterAnimator.gameObject.SetActive(true);

            if (_spawnedRagdoll)
                Destroy(_spawnedRagdoll.gameObject);
        }

        void ServerHealthDepleted (CharacterPart damagedPart, AttackType attackType, Health attacker, int attackForce) 
        {
            CharacterInstance characterInstance = GetComponent<CharacterInstance>();

            _spawnedRagdoll = SpawnRagdoll(characterInstance.CharacterAnimator.MySkin);

            Vector3 movementDirection = transform.rotation * new Vector3(characterInstance.Input.Movement.x, 0, characterInstance.Input.Movement.y);

            _spawnedRagdoll.ServerActivateRagdoll(characterInstance.Health.GetPositionToAttack(), attacker.GetPositionToAttack(), movementDirection * (characterInstance.ReadActionKeyCode(ActionCodes.Sprint) ? 2f : 1f), attackForce);

            GetComponent<RagDollSyncer>().ServerStartSynchronizingRagdoll(_spawnedRagdoll.GetComponent<RagDoll>());
        }

        void CheckHealthState(int currentHealth, CharacterPart damagedPart, AttackType attackType, byte attackerID)
        {
            CharacterInstance characterInstance = GetComponent<CharacterInstance>();

            _audioSource.Stop();

            //play headshot clip when hitted in head, plays always when character receives damage, not only for death
            if (damagedPart == CharacterPart.head)
            {
                PooledObject headImpact = _headImpactEffectPool.ReturnObject(_head.position, _head.rotation);

                Health attacker = GameSync.Singleton.Healths.GetObj(attackerID);

                if(attacker)
                    headImpact.transform.LookAt(attacker.GetPositionToAttack());

                _audioSource.PlayOneShot(_impactClip_head);
            }
            else
                _audioSource.PlayOneShot(_impactClip_body);

            //executes only on death, hides player model and spawns ragdoll
        }
        private void ClientOnHealthDepleted(CharacterPart damagedPart, byte attackerID)
        {
            _characterInstance.CharacterAnimator.RenderCharacterModel(false);

            _spawnedRagdoll = SpawnRagdoll(_characterInstance.CharacterAnimator.MySkin);
            _spawnedRagdoll.ActivateRagdoll(damagedPart);

            if (damagedPart==CharacterPart.head)
            {
                PooledObject headBlood = _headBloodEffectPool.ReturnObject(_spawnedRagdoll._head.transform.position, _spawnedRagdoll._head.transform.rotation);
                headBlood.transform.LookAt(GameSync.Singleton.Healths.GetObj(attackerID).GetPositionToAttack());
                headBlood.GetComponent<PooledParticleSystem>().SetPositionTarget(_spawnedRagdoll._head.transform);
            }

            _characterInstance.ObjectForDeathCameraToFollow = _spawnedRagdoll._head.transform;

            GetComponent<RagDollSyncer>().AssignRagdoll(_spawnedRagdoll);
        }

        RagDoll SpawnRagdoll(SkinContainer skin) 
        {
            if (!_spawnedRagdoll)
            {
                _spawnedRagdoll = Instantiate(ragDoll_Prefab, transform.position, transform.rotation).GetComponent<RagDoll>();
                _spawnedRagdoll.transform.SetParent(transform);

                _spawnedRagdoll.ApplySkin(skin);
                return _spawnedRagdoll;
            }
            else 
            {
                return _spawnedRagdoll;
            }
        }
    }
}