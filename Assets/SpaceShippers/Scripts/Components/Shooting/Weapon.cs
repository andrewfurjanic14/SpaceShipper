using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "Scriptable Objects/Weapon")]
public class Weapon : ScriptableObject
{
    public Projectile ProjectilePrefab;
    public float FireRate = 3f;
    public float Damage = 10f;
    public float SpreadDegrees = 0f;
    public float ProjectileSpeed = 60f;
    public float ProjectileLifetime = 4f;
}
