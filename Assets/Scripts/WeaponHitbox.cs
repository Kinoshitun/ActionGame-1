using UnityEngine;
using System.Collections.Generic;

public class WeaponHitbox : MonoBehaviour
{
    public int damage = 10;
    private Collider hitboxCollider;
    private CharacterCombat combatController;
    
    private List<Collider> alreadyHitTargets = new List<Collider>();    // 1回の攻撃で既に当たった相手を記憶しておくリスト

    void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.enabled = false;
    }

    public void Initialize(CharacterCombat combat)
    {
        combatController = combat;
    }

    // 剣を振り下ろす瞬間に CharacterCombat から呼ばれる
    public void EnableHitbox()
    {
        hitboxCollider.enabled = true;
        alreadyHitTargets.Clear(); // 攻撃ごとにリストをリセット
    }

    // 振り終わったら呼ばれる
    public void DisableHitbox()
    {
        hitboxCollider.enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || alreadyHitTargets.Contains(other)) return;    //プレイヤー自身やすでに当たった相手は無視

        IDamageable target = other.GetComponentInParent<IDamageable>();     //当たった相手がIDamageableを持っているかチェック
        if (target != null)
        {
            alreadyHitTargets.Add(other);

            Vector3 hitPoint = other.ClosestPoint(transform.position);      //ヒット座標を計算
            target.TakeDamage(damage, hitPoint);                            //ダメージ付与

            if (combatController != null)
            {
                combatController.TriggerHitStop();      // ヒットストップ実行
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
