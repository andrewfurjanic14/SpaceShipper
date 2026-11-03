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
        private DamageableEntity health = null; 
        
        private void Awake()
        {
            controller = GetComponent<EnemyMovement>();
            health = GetComponent<DamageableEntity>();
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
    }
}