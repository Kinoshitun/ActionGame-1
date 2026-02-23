using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float chaseRange = 10f;    // プレイヤーに気づいて追いかけ始める距離
    [SerializeField] private float attackRange = 2f;    // 攻撃を振るう距離
    [SerializeField] private float attackCooldown = 2f; // 攻撃と攻撃の間隔（秒）
    [SerializeField] private float rotationSpeed = 5f;  // 振り向くスピード

    [Header("Combat Settings")]
    [SerializeField] private EnemyWeaponHitbox weaponHitbox;

    private NavMeshAgent agent;
    private Animator animator;
    private Transform playerTransform;

    private float lastAttackTime;
    private bool isAttacking;
    private bool isHitStunned; // 被弾中の硬直状態

    private readonly int animSpeedHash = Animator.StringToHash("Speed");
    private readonly int animAttackStateHash = Animator.StringToHash("Attack");
    private readonly int animLocomotionStateHash = Animator.StringToHash("Locomotion");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        // プレイヤーをタグで探す
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        if (agent != null)
        {
            // 攻撃範囲の少し手前（0.5m手前）で止まるようにする
            agent.stoppingDistance = Mathf.Max(attackRange - 0.5f, 0.5f);
        }
    }

    private void Update()
    {
        // プレイヤーがいない、または攻撃中・被弾中はAIの思考をストップ
        if (playerTransform == null || isAttacking || isHitStunned) 
        {
            if (agent.enabled) agent.isStopped = true;
            if (animator != null) animator.SetFloat(animSpeedHash, 0f);
            return;
        }

        // プレイヤーとの距離を測る
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= attackRange)
        {
            // 攻撃範囲内に入った
            agent.isStopped = true;
            animator.SetFloat(animSpeedHash, 0f); // 足を止める

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PerformAttack();
            }
            else
            {
                // クールダウン中は、プレイヤーの方をジッと見つめて隙を窺う
                FaceTarget(playerTransform.position);
            }
        }
        else if (distanceToPlayer <= chaseRange)
        {
            // 追跡範囲内（走って追いかける）
            agent.isStopped = false;
            agent.SetDestination(playerTransform.position);
            
            // NavMeshAgentの現在の移動スピードをAnimatorに渡す
            if (animator != null) animator.SetFloat(animSpeedHash, agent.velocity.magnitude);
        }
        else
        {
            // 範囲外（見失っている状態なので立ち止まる）
            agent.isStopped = true;
            if (animator != null) animator.SetFloat(animSpeedHash, 0f);
        }
    }

    private void PerformAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        
        // 攻撃する瞬間にプレイヤーの方を向く
        FaceTarget(playerTransform.position);

        if (animator != null)
        {
            animator.CrossFadeInFixedTime(animAttackStateHash, 0.1f);
        }
    }

    public void OnAttackEnd()
    {
        isAttacking = false;
        
        if (animator != null && !isHitStunned)
        {
            animator.CrossFadeInFixedTime(animLocomotionStateHash, 0.2f);
        }
    }

    // EnemyHealthから被弾時に呼ばれる
    public void OnHit()
    {
        isHitStunned = true;
        isAttacking = false;
        if (agent.enabled) agent.isStopped = true;
        
        // 前のタイマーをキャンセルして、0.5秒後に硬直を解除する
        CancelInvoke(nameof(EndHitStun));
        Invoke(nameof(EndHitStun), 0.5f); 
    }

    public void EnableWeaponHitbox()
    {
        weaponHitbox.EnableHitbox();
    }

    public void DisableWeaponHitbox()
    {
        weaponHitbox.DisableHitbox();
    }

    private void EndHitStun()
    {
        isHitStunned = false;

        if (animator != null && !isAttacking)
        {
            animator.CrossFadeInFixedTime(animLocomotionStateHash, 0.2f);
        }
    }

    private void FaceTarget(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        direction.y = 0; // 上下方向には向かないようにする
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // インスペクター上で、追跡範囲と攻撃範囲を可視化する（選択時のみ）
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
