using UnityEngine;

public enum EntityControlType
{
    PlayerControlled,
    EnemyControlled
}

public class DamageableEntity : MonoBehaviour
{
    [SerializeField] private EntityControlType controlType;
    [SerializeField] private float health;
    [SerializeField] private uint teamID;
}
