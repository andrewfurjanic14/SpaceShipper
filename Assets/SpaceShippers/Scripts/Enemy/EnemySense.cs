using UnityEngine;

namespace SpaceShippers.Scripts.Enemy
{
    public enum SenseType
    {
        None,
        Sight,
        Radar,
    }
    
    [CreateAssetMenu(fileName = "Sense", menuName = "Scriptable Objects/Enemy Sense", order = 0)]
    public class EnemySense : ScriptableObject
    {
        /// <summary>
        /// The maximum distance at which the enemy can sense targets. For example, a value of 100 means the enemy can detect targets up to 100 units away.
        /// </summary>
        [SerializeField] private float _senseRange = 100f;
        
        /// <summary>
        /// The direction in which the enemy is "looking" for targets. This is a local direction relative to the enemy's transform. For example, Vector3.forward means the enemy will sense targets in front of it.
        /// </summary>
        [SerializeField] private Vector3 _senseDirection = Vector3.forward;
        
        /// <summary>
        /// The angle of the sense cone. For example, a value of 45 means the enemy can detect targets within a 45-degree cone in front of it.
        /// </summary>
        [SerializeField] private float _senseAngle = 45f;
        
        /// <summary>
        /// The type of sense this EnemySense represents. This can be Sight, Radar, or any other sense you want to implement in the future.
        /// </summary>
        [SerializeField] public SenseType _senseType = SenseType.None;
        
        /// <summary>
        /// Layer mask for the sense. This determines which layers the sense can detect. For example, if you want the enemy to only detect the player, you would set this to the player's layer.
        /// </summary>
        [SerializeField] private LayerMask _senseMask;
        
        /// <summary>
        /// Chance to take action when sense is triggered (0-1). A value of 1 means the enemy will always take action when the sense is triggered, while a value of 0 means the enemy will never take action.
        /// </summary>
        [SerializeField] private float _actionChance = 1f; // chance to take action when sense is triggered (0-1)
        
        /// <summary>
        /// Noise factor for the sense. This will add random variation to the sense.
        /// </summary>
        [SerializeField] private Vector3 _senseNoise = Vector3.zero;

        /// <summary>
        /// Confidence level for the sense. This can be used to determine how certain the enemy is about the detected target. A value of 1 means the enemy is fully confident, while a value of 0 means the enemy has no confidence in the detection.
        /// </summary>
        [SerializeField] public float confidenceLevel = 0.5f;
        
        /// <summary>
        /// Checks if the sense is triggered and returns the position of the detected target. If no target is detected, it returns the origin position. The method also applies noise to the detected position based on the _senseNoise variable.
        /// </summary>
        /// <param name="origin">The origin of the sense.</param>
        /// <returns>The position of the detected target, or the origin position if no target is detected.</returns>
        public Vector3 CheckTrigger(Transform origin)
        {
            if (Random.Range(0f, 1f) > _actionChance)
            {
                return origin.position; // No action taken due to chance
            }
            
            var r = _senseType switch
            {
                SenseType.Sight => CheckSight(origin),
                SenseType.Radar => CheckRadar(origin),
                _ => origin.position
            };
            
            return r;
        }

        private Vector3 CheckSight(Transform origin)
        {
            var direction = origin.TransformDirection(_senseDirection);
            var hits = Physics.OverlapSphere(origin.position, _senseRange, _senseMask);
            var noise = new Vector3(Random.Range(-_senseNoise.x, _senseNoise.x), Random.Range(-_senseNoise.y, _senseNoise.y), Random.Range(-_senseNoise.z, _senseNoise.z));
            
            foreach (var hit in hits)
            {
                if (!hit) continue;
                
                if (hit.transform == origin) continue; // Ignore self
                
                if (Physics.Raycast(origin.position, (hit.transform.position - origin.position).normalized, out RaycastHit rayHit, (hit.transform.position - origin.position).magnitude))
                {
                    if (rayHit.transform != hit.transform) continue; // Something is blocking the view
                }
                
                var toTarget = hit.transform.position - origin.position;
                if (Vector3.Angle(direction, toTarget) <= _senseAngle)
                {
                    // Target is within the sense angle
                    return hit.transform.position + noise;
                }
            }
            return origin.position; // No target found, return origin position
        }
        
        private Vector3 CheckRadar(Transform origin)
        {
            var hits = Physics.OverlapSphere(origin.position, _senseRange, _senseMask);
            var noise = new Vector3(Random.Range(-_senseNoise.x, _senseNoise.x), Random.Range(-_senseNoise.y, _senseNoise.y), Random.Range(-_senseNoise.z, _senseNoise.z));
            if (hits.Length > 0)
            {
                // Return the position of the first detected target
                return hits[0].transform.position + noise;
            }
            return origin.position; // No target found, return origin position
        }
    }
}