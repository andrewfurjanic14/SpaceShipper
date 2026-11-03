using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public enum EntityControlType
{
    PlayerControlled, EnemyControlled
}

public class DamageableEntity : MonoBehaviour
{
    [SerializeField] private EntityControlType controlType;
    [SerializeField] private float health = 100f;
    [SerializeField] private uint teamID;

    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnDeath;

    private PlayerMovement MovementController;

    public EntityControlType ControlType => controlType;
    public uint TeamID => teamID;
    public float Health => health;
    public bool IsDead { get; private set; }
    
    [SerializeField] public float impactDamageMultiplier = 0.1f;

    private void Awake()
    {
        MovementController = GetComponent<PlayerMovement>();
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        health = Mathf.Max(0f, health -amount);
        OnHealthChanged?.Invoke(health);

        if (health <= 0f)
            Die();
    }

    private void Die()
    {
        IsDead = true;
        OnDeath?.Invoke();
        
        GameObject[] planets = GameObject.FindGameObjectsWithTag("Terrestrial");
        var rb = GetComponent<Rigidbody>();
        foreach (GameObject planet in planets)
        {
            var gravField = planet.GetComponent<GravField>();
            if (gravField != null)
            {
                gravField.RemoveObjectFromGravField(rb);
            }
        }

        if (controlType == EntityControlType.EnemyControlled)
        {
            if (gameObject.layer == LayerMask.NameToLayer("Debris"))
            {
                Destroy(gameObject);
                return;
            }
            gameObject.layer = LayerMask.NameToLayer("Debris");
        }
        else
        {
            // replace game-over screen later.
            gameObject.SetActive(false);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (MovementController && MovementController.IsDashing) return;
        TakeDamage(collision.relativeVelocity.magnitude * impactDamageMultiplier);
    }
}