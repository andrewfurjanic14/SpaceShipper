using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private Transform player;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float stoppingDistance = 8f;
    [SerializeField] private float shootingRange = 15f;

    [SerializeField] private float obstacleCheckDistance = 3f;

    private Weapon weapon;
    private Rigidbody rb;

    void Awake()
    {
        weapon = GetComponentInChildren<Weapon>();
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;
    }

    void Update()
    {
        if (player == null) return;

        float distance =
            Vector3.Distance(transform.position, player.position);

        if (distance <= shootingRange && weapon != null)
        {
            weapon.TryFire();
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;

        Vector3 direction =
            player.position - rb.position;

        float distance = direction.magnitude;

        if (distance < 0.001f) return;

        // Face player
        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        rb.MoveRotation(Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        ));

        // Detect obstacles ahead
        bool obstacleAhead = Physics.Raycast(
            rb.position,
            direction.normalized,
            obstacleCheckDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        // Move only if path is clear
        if (distance > stoppingDistance && !obstacleAhead)
        {
            Vector3 nextPosition = Vector3.MoveTowards(
                rb.position,
                player.position,
                moveSpeed * Time.fixedDeltaTime
            );

            rb.MovePosition(nextPosition);
        }
    }
}
