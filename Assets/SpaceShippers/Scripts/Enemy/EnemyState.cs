using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SpaceShippers.Scripts.Enemy
{
    [Serializable]
    public struct ActionPackage
    {
        public Vector3 targetPosition;
        public SenseType senseType;
        public float responseLevel;
        public bool targetFound;
        public EnemyState nextState;
        public bool transitionToNextState;
    }
    
    [Serializable]
    public struct TransitionPackage
    {
        public EnemySense sense;
        public EnemyState nextState;
    }
    
    [CreateAssetMenu(fileName = "Behavior", menuName = "Scriptable Objects/Enemy Behavior", order = 0)]
    public class EnemyState : ScriptableObject
    {
        [SerializeField] private List<TransitionPackage> _transitionPackages = new List<TransitionPackage>();
        
        [SerializeField] private EnemySense currentTrackSense;
        [SerializeField] private bool isDefaultState = false;
        [SerializeField] private float aggressionLevel = 1.0f;
        
        [Header("State Settings")]
        [SerializeField] public Weapon stateWeapon = null;
        [SerializeField] public bool allowMovement = true;
        
        [Range(0f, 1f)]
        [SerializeField] private float stateChangeChance = 0.1f; // Random chance to change states
        
        [TextArea(3, 5)]
        [SerializeField] private string _comments = "Add comments here to describe the purpose of this state and its behavior.";

        // Public properties for external access
        public Weapon StateWeapon => stateWeapon;
        public bool AllowMovement => allowMovement;
        public bool IsDefaultState => isDefaultState;

        public ActionPackage TickState(Transform origin)
        {
            var ap = new ActionPackage
            {
                targetPosition = currentTrackSense ? currentTrackSense.CheckTrigger(origin) : origin.position,
                senseType = currentTrackSense ? currentTrackSense._senseType : SenseType.None,
                transitionToNextState = false
            };
            
            ap.targetFound = ap.targetPosition != origin.position;
            
            if (currentTrackSense)
            {
                ap.responseLevel = aggressionLevel * currentTrackSense.confidenceLevel;
            }

            var stateFound = false; 
            
            if (!ap.targetFound)
            {
                foreach (var package in _transitionPackages)
                {
                    var sense = package.sense;
                    var nextState = package.nextState;
                    
                    if (sense)
                    {
                        var sensedPosition = sense.CheckTrigger(origin);
                        if (sensedPosition != origin.position)
                        {
                            ap.targetPosition = sensedPosition;
                            ap.senseType = sense._senseType;
                            ap.nextState = nextState;
                            ap.responseLevel = aggressionLevel * sense.confidenceLevel;
                            ap.transitionToNextState = true; 
                            stateFound = true;
                        }
                    }
                    
                    if (nextState && nextState.IsDefaultState && !stateFound)
                    {
                        ap.nextState = nextState;
                        ap.transitionToNextState = true;
                        stateFound = true; 
                    }
                }

                if (!stateFound && _transitionPackages.Count > 0 && Random.Range(0f, 1f) < stateChangeChance)
                {
                    var index = Random.Range(0, _transitionPackages.Count);
                    var package = _transitionPackages[index];
                    ap.nextState = package.nextState;
                    ap.transitionToNextState = true;
                }
            }
            
            return ap;
        }
    }
}