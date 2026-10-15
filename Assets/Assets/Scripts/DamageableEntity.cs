using UnityEngine;
using UnityEngine.Events;

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

    public EntityControlType ControlType => controlType;
    public uint TeamID => teamID;
    public float Health => health;
    public bool IsDead { get; private set; }

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

        if (controlType == EntityControlType.EnemyControlled)
        {
            Destroy(gameObject);
        }
        else
        {
            // replace game-over screen later.
            gameObject.SetActive(false);
        }
    }
}