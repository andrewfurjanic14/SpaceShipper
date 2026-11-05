using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
public class EnemyShooting : MonoBehaviour
{
    [SerializeField] private Shooter _shooter;
    [SerializeField] private float fireRange = 40f;
    [SerializeField] private float aimTolerance = 10f; //degrees
    private EnemyMovement movement;

    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();

        if (_shooter == null)
            _shooter = GetComponentInChildren<Shooter>();
    }

    private void Update()
    {
        Transform target = movement.Target;
        bool shouldFire = false;

        if (target != null && target.gameObject.activeInHierarchy)
        {
            Vector3 toTarget = target.position - transform.position;
            shouldFire = toTarget.magnitude <= fireRange && Vector3.Angle(transform.forward, toTarget) <= aimTolerance;
        }

        if (shouldFire)
        {
            _shooter.StartFiring();
        }
        else
        {
            _shooter.StopFiring();
        }
    }
}