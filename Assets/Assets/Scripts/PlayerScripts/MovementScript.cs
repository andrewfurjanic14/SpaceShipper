using System;
using UnityEngine;

public class MovementScript : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float moveSpeed, dashSpeed;
    
    public void Move(Vector3 direction)
    {
        if (PlayerController.Instance.CurrentState.CanMove)
        {
            rb.linearVelocity = direction * moveSpeed;
        }
    }

    public void Dash(Vector3 direction)
    {
        if (PlayerController.Instance.CurrentState.CanDash)
        {
            rb.linearVelocity = direction * dashSpeed;
        }
    }
}
