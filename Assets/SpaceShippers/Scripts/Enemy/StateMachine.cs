using System;
using UnityEngine;

namespace SpaceShippers.Scripts.Enemy
{
    enum enemyDefaultState
    {
        Idle,
        Patrol,
    }
    
    [RequireComponent(typeof(EnemyMovement))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Shooter))]
    public class StateMachine : MonoBehaviour
    {
        [SerializeField] private LayerMask targetMask;
        [SerializeField] private ItemTable _debrisTable;
        [SerializeField] private float _debrisHealth = 50f;
        [SerializeField] private enemyDefaultState _initialState;
        [SerializeField] private EnemyState _behavior;
        [SerializeField] private float _acceptedAgressionLevel = 0.5f; // The minimum aggression level required to fire the weapon
        [SerializeField] private float _acceptedFireAngle = 10f; // The maximum angle difference between the enemy's forward direction and the target position to fire the weapon
        
        private EnemyMovement controller = null;
        private enemyDefaultState currentState = enemyDefaultState.Idle;
        private Health health = null; 
        private bool isDebris = false;
        private Shooter shooter = null;
        
        private void Awake()
        {
            controller = GetComponent<EnemyMovement>();
            health = GetComponent<Health>();
            shooter = GetComponent<Shooter>();
            health.Died += KillMe;
        }

        private void Start()
        {
            currentState = enemyDefaultState.Patrol;
        }

        private void Update()
        {
            if (health && health.IsDead)
            {
                return;
            }

            if (isDebris) return; 
            
            var deltaTime = Time.deltaTime;

            var package = _behavior.TickState(transform);
            
            if (package.transitionToNextState)
            {
                _behavior = package.nextState ? package.nextState : _behavior;
            }

            if (!package.targetFound)
            {
                if (_behavior.allowMovement)
                {
                    controller.Patrol(deltaTime);
                }
                else
                {
                    controller.Idle(deltaTime);
                }
            }
            else
            {
                controller.MoveTowards(package.targetPosition, deltaTime);
                TryFireWeapon(_behavior.stateWeapon, package.targetPosition, package.responseLevel); 
            }
        }
        
        private void KillMe()
        {
            if (health)
            {
                health.Died -= KillMe;
            }

            isDebris = true;
            
            gameObject.layer = LayerMask.NameToLayer("Debris");
            if (_debrisTable)
            {
                var r = gameObject.AddComponent<ResourceNode>();
                r._itemTable = _debrisTable;            
                health.Revive(_debrisHealth);
            }

            enabled = false; 
        }

        private void TryFireWeapon(Weapon weapon, Vector3 targetPosition, float responseLevel)
        {
            if (!weapon) return; 
            
            shooter._weapon = weapon;

            if (Vector3.Angle(transform.forward, targetPosition - transform.position) < _acceptedFireAngle &&
                responseLevel >= _acceptedAgressionLevel) shooter.TryFire();
        }
    }
}