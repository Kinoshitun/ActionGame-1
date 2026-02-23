using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int damage, Vector3 hitPoint);
    Transform TargetPoint { get; }
}
