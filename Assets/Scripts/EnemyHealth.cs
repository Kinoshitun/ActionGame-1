using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHP = 100;
    private int currentHP;

    [Header("Targeting")]
    [Tooltip("ロックオンマーカーを表示する位置")]
    [SerializeField] private Transform targetPoint;
    public Transform TargetPoint => targetPoint != null ? targetPoint : transform;

    [Header("UI")]
    [SerializeField] private UIHealthBar hpBar;

    private Animator animator;
    private EnemyAI enemyAI;

    private bool isLockedOnByPlayer = false;

    private readonly int animHitStateHash = Animator.StringToHash("Hit");
    private readonly int animDieStateHash = Animator.StringToHash("Die");

    private void Awake()
    {
        currentHP = maxHP;
        animator = GetComponentInChildren<Animator>();
        enemyAI = GetComponent<EnemyAI>();
    }

    private void Start()
    {
        hpBar.Initialize(maxHP);
    }

    public void TakeDamage(int damage, Vector3 hitPoint)
    {
        if (currentHP <= 0) return;

        currentHP -= damage;
        Debug.Log($"{gameObject.name} に {damage} のダメージ！ 残りHP: {currentHP}");

        hpBar.UpdateHP(currentHP);
        hpBar.ShowDamageText(damage);

        hpBar.ShowHealthBar(true);
        if (!isLockedOnByPlayer)
        {
            CancelInvoke(nameof(HideHealthBar));
            Invoke(nameof(HideHealthBar), 3f);
        }

        // ヒットエフェクトを出したい場合は、ここで hitPoint の座標にパーティクルを生成します

        if (currentHP <= 0)
        {
            Die();
        }
        else
        {
            if (animator != null) animator.CrossFadeInFixedTime(animHitStateHash, 0.1f);    // 被弾アニメーションの再生
            if (enemyAI != null) enemyAI.OnHit();   // 被弾中はAIの移動や攻撃を一時中断させる
        }
    }

    private void Die()
    {
        if (animator != null) animator.CrossFadeInFixedTime(animDieStateHash, 0.1f);

        // AIの思考を停止させる
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();    // NavMeshAgentも停止させる
            if (agent != null) agent.enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;       // ロックオンの対象から外れるように、当たり判定（Collider）を消す

        hpBar.gameObject.SetActive(false);
        
        // 必要に応じて、数秒後に死体を消滅させることも可能です
        // Destroy(gameObject, 5f);
    }

    public void SetLockOnState(bool isLockedOn)     // TargetingSystemから呼ばれ、ロックオン中の表示を管理する
    {
        isLockedOnByPlayer = isLockedOn;
        
        if (hpBar != null)
        {
            if (isLockedOn)
            {
                hpBar.ShowHealthBar(true);
                CancelInvoke(nameof(HideHealthBar)); // ロックオン中は消えないようにタイマーをキャンセル
            }
            else
            {
                HideHealthBar(); // ロックオンが外れたら隠す
            }
        }
    }

    private void HideHealthBar()
    {
        if (hpBar != null && currentHP > 0)     // まだ生きている場合のみ隠す
        {
            hpBar.ShowHealthBar(false);
        }
    }
}
