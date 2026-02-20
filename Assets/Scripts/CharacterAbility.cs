using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CharacterAbility : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private CharacterMovement movement;
    [SerializeField] private CharacterCombat combat;
    [SerializeField] private PlayerController player;

    public bool IsUsingAbility { get; private set; }

    [Header("Energy")]
    [SerializeField] private Slider energyBar;
    [SerializeField] private float maxEnergy = 100f;
    private float currentEnergy = 0f;

    [Header("Ability - Drain")]
    [SerializeField] private float drainRadius = 8f;
    [SerializeField] private float drainCooldown = 1.0f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject EnergyOrbPrefab;
    [SerializeField] private GameObject drainAreaEffect;
    private float lastDrainTime;

    [Header("Ability - Dash Strike")]
    [SerializeField] private float strikeEnergyCost = 50f;
    [SerializeField] private float strikeDashSpeed = 30f;
    [SerializeField] private float strikeDuration = 0.3f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float knockbackForce = 20f;
    [SerializeField] private GameObject explosionVFXPrefab;
    private bool isDashStriking = false;

    private Animator animator;
    private Transform cameraTransform;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        if (movement == null) movement = GetComponent<CharacterMovement>();
        if (combat == null) combat = GetComponent<CharacterCombat>();
        if (player == null) player = GetComponent<PlayerController>();
        if (Camera.main != null) cameraTransform = Camera.main.transform;
    }

    // public void AddEnergy(float amount)
    // {
    //     currentEnergy += amount;
    //     if (currentEnergy > maxEnergy) currentEnergy = maxEnergy;
    //     if (currentEnergy < 0) currentEnergy = 0;

    //     if (energyBar != null) energyBar.value = currentEnergy;
    // }
    
    // public void UseDrain()
    // {
    //     if (Time.time - lastDrainTime < drainCooldown) return;
    //     lastDrainTime = Time.time;

    //     // エフェクト生成
    //     if (drainAreaEffect != null)
    //     {
    //         GameObject effect = Instantiate(drainAreaEffect, transform.position, Quaternion.identity);
    //         float diameter = drainRadius * 2;
    //         effect.transform.localScale = new Vector3(drainRadius * 2, effect.transform.localScale.y, diameter);
    //         Destroy(effect, 0.5f);
    //     }

    //     //animator.CrossFadeInFixedTime("Drain", 0.1f);

    //     Collider[] enemies = Physics.OverlapSphere(transform.position, drainRadius, enemyLayer);
    //     foreach (Collider col in enemies)
    //     {
    //         EnemyDummy enemy = col.GetComponent<EnemyDummy>();
    //         if (enemy != null)
    //         {
    //             enemy.OnDrain();

    //             if (EnergyOrbPrefab != null)
    //             {
    //                 GameObject orb = Instantiate(EnergyOrbPrefab, col.transform.position + Vector3.up, Quaternion.identity);
    //                 EnergyOrb orbScript = orb.GetComponent<EnergyOrb>();
    //                 if (orbScript != null) orbScript.Initialize(this.transform);
    //             }
    //         }
    //     }
    // }

    // public void UseDashStrike()
    // {
    //     if (currentEnergy >= strikeEnergyCost && movement.IsGrounded && !isDashStriking && !combat.IsAttacking)
    //     {
    //         StartCoroutine(DashStrikeRoutine());
    //     }
    //     else
    //     {
    //         Debug.Log("今は技を出せない！");
    //     }
    // }

    // public void UseDodge(Vector2 inputDir, bool isSprinting)
    // {
    //     // クールダウンと状態チェック
    //     if (Time.time - lastDodgeTime < dodgeCooldown && !IsUsingAbility && !combat.IsAttacking) return;

    //     StartCoroutine(DodgeRoutine(inputDir, isSprinting));
    // }

    // private IEnumerator DashStrikeRoutine()
    // {
    //     if (!player.TryExecuteAction(ActionPriority.Ability)) yield break;

    //     IsUsingAbility = true;
    //     isDashStriking = true;
    //     combat.IsInvincible = true;
    //     AddEnergy(-strikeEnergyCost);

    //     animator.CrossFadeInFixedTime("DashStrike", 0.1f);

    //     float startTime = Time.time;
    //     Vector3 dashDirection = transform.forward;

    //     RaycastHit hitInfo = new RaycastHit();
    //     bool hitSomething = false;

    //     //突進フェーズ
    //     while (Time.time < startTime + strikeDuration)
    //     {
    //         //高速移動
    //         movement.ForceMove(dashDirection * strikeDashSpeed * Time.deltaTime);
    //         //衝突判定
    //         if (Physics.SphereCast(transform.position + Vector3.up, 0.5f, dashDirection, out hitInfo, 1.0f, enemyLayer))
    //         {
    //             hitSomething = true;
    //             break;  //衝突したら即爆発へ
    //         }
    //         yield return null;
    //     }

    //     //爆発フェーズ
    //     movement.ForceMove(Vector3.zero);//停止

    //     if (hitSomething)
    //     {
    //         Time.timeScale = 0.1f;
    //         yield return new WaitForSecondsRealtime(0.1f);
    //         Time.timeScale = 1.0f;
    //     }

    //     Vector3 explosionPosition = hitSomething ? hitInfo.point : transform.position + transform.forward * 2f;

    //     //エフェクト出す
    //     if (explosionVFXPrefab != null)
    //     {
    //         GameObject vfx = Instantiate(explosionVFXPrefab, explosionPosition, Quaternion.identity);
    //         Destroy(vfx, 2.0f);
    //     }

    //     //吹き飛ばし
    //     Collider[] hitColliders = Physics.OverlapSphere(explosionPosition, explosionRadius, enemyLayer);
    //     foreach (Collider col in hitColliders)
    //     {
    //         Rigidbody rb = col.GetComponent<Rigidbody>();
    //         if (rb != null)
    //         {
    //             Vector3 knockbackDir = (col.transform.position - explosionPosition).normalized + Vector3.up * 0.5f;
    //             rb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
    //         }
    //     }

    //     //硬直時間
    //     yield return new WaitForSeconds(0.1f);
        
    //     combat.IsInvincible = false;
    //     isDashStriking = false;
    //     IsUsingAbility = false;
    // }

    

    void OnDrawGizmosSelected()
    {
        //ドレイン範囲の可視化
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, drainRadius);

        
    }
}
