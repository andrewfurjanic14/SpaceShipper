using System;
using UnityEngine;

namespace SpaceShippers.Scripts.Enemy
{
    enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack
    }
    
    public class StateMachine : MonoBehaviour
    {
        [SerializeField] private LayerMask targetMask;
        
        private EnemyMovement controller = null;
        private EnemyState currentState = EnemyState.Idle;
        private Health health = null; 
        private bool isDebris = false;
        
        private void Awake()
        {
            controller = GetComponent<EnemyMovement>();
            health = GetComponent<Health>();
            health.Died += KillMe;
        }

        private void Start()
        {
            currentState = EnemyState.Patrol;
        }

        private void Update()
        {
            if (health && health.IsDead)
            {
                return;
            }

            if (isDebris) return; 
            
            var deltaTime = Time.deltaTime; 
            switch (currentState)
            {
                case EnemyState.Idle:
                    controller.Idle(deltaTime);
                    break;
                case EnemyState.Patrol:
                    controller.Patrol(deltaTime);
                    break;
                case EnemyState.Chase:
                    //controller.Chase(deltaTime);
                    break;
                case EnemyState.Attack:
                    //controller.Attack(deltaTime);
                    break;
            }
        }
        
        private void KillMe()
        {
            if (health)
            {
                health.Died -= KillMe;
            }

            isDebris = true; 
            health.MakeDebris();
        }
    }
}