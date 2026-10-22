using UnityEngine;
using static UnityEngine.GraphicsBuffer;

// automatically add a ridgidbody
[RequireComponent(typeof(Rigidbody))]
public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private Transform target;
    public Transform Target => target; // for shooting script to read the target

    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float turnSpeed = 180f;
    [SerializeField] private float stopDistance = 3f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void Start()
    {
        if(Random.value < 0.5f)
        {
            target = GameObject.Find("cargoship").transform;
        } else
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            target = player.transform;
        }

    }

    private void FixedUpdate()
    {
        // no target, or the player died 
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 toTarget = target.position - rb.position;
        float distance = toTarget.magnitude;

        if (distance > 0.001f)
        {
            Vector3 direction = toTarget / distance;

            // Face the player
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.RotateTowards(
                rb.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));

            // move toward the player, stop when close enough
            rb.linearVelocity = distance > stopDistance ? direction * moveSpeed : Vector3.zero;
        }
    }
}