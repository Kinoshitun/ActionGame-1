using UnityEngine;
using System.Collections.Generic;

public class EnemyWeaponHitbox : MonoBehaviour
{
    public int damage = 10;
    private Collider hitbox;
    private List<Collider> alreadyHitTargets = new List<Collider>();

    void Awake()
    {
        hitbox = GetComponent<Collider>();
        hitbox.isTrigger = true;
        hitbox.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public void EnableHitbox()
    {
        hitbox.enabled = true;
        alreadyHitTargets.Clear();
    }

    public void DisableHitbox()
    {
        hitbox.enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        // 敵同士や既に当たった相手は無視
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy") || alreadyHitTargets.Contains(other)) return;

        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            alreadyHitTargets.Add(other);
            Vector3 hitPosition = other.ClosestPoint(transform.position);
            target.TakeDamage(damage, hitPosition);
        }
    }
}
