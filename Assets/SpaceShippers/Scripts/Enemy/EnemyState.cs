using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceShippers.Scripts.Enemy
{
    [Serializable]
    public struct ActionPackage
    {
        public Vector3 targetPosition;
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
            // First check the primary sense for this state
            var primaryStateSense = currentTrackSense.CheckTrigger(origin);
            
            var ap = new ActionPackage
            {
                targetPosition = primaryStateSense.targetLocation,
                transitionToNextState = false,
                targetFound = primaryStateSense.sensorTriggered,
                responseLevel = primaryStateSense.confidenceLevel
            };

            // Check all transition packages for potential state changes (if they can find a better target)
            foreach (var sensor in _transitionPackages)
            {
                if (sensor.sense && sensor.nextState)
                {
                    var senseResult = sensor.sense.CheckTrigger(origin);
                    if (senseResult.sensorTriggered && senseResult.confidenceLevel > ap.responseLevel)
                    {
                        ap.targetPosition = senseResult.targetLocation;
                        ap.responseLevel = senseResult.confidenceLevel;
                        ap.transitionToNextState = true;
                        ap.nextState = sensor.nextState;
                    }

                    if (!ap.targetFound && sensor.nextState.IsDefaultState)
                    {
                        ap.nextState = sensor.nextState;
                        ap.transitionToNextState = true;
                    }
                }
            }
            
            ap.responseLevel*= aggressionLevel; // Apply the state's aggression level to the response level
            
            return ap;
        }
    }
}