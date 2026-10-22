using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]

public class EnemyShooting : MonoBehaviour
{
    [SerializeField] private Weapon weapon;
    [SerializeField] private float fireRange = 40f;
    [SerializeField] private float aimTolerance = 10f; //degrees
    private EnemyMovement movement;

    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();

        if (weapon == null)
            weapon = GetComponentInChildren<Weapon>();
    }

    private void Update()
    {
        Transform target = movement.Target;
        if (weapon == null || target == null || !target.gameObject.activeInHierarchy)
            return;

        Vector3 toTarget = target.position - transform.position;

        // fire when close enough and roughly facing the player
        if (toTarget.magnitude > fireRange) return;
        if (Vector3.Angle(transform.forward, toTarget) > aimTolerance) return;

        weapon.TryFire();
    }
}