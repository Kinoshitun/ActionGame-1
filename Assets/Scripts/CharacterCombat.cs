using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

public class CharacterCombat : MonoBehaviour
{
    private CharacterAnimator charAnim;
    private PlayerController playerController;

    // [SerializeField] private float maxHealth = 100f;
    // private float currentHealth;

    [Header("Weapon Settings")]
    [SerializeField] private WeaponHitbox weaponHitbox;

    [System.Serializable]
    public struct AttackSettings
    {
        public float duration;         // アニメーション全体の長さ
        public float cancelableTime;   // 次の攻撃でキャンセル可能になる時間
        public float hitboxStartTime;  // 剣を振り下ろす瞬間（判定ON）
        public float hitboxEndTime;    // 剣を振り切った瞬間（判定OFF）
        public float rotationEndTime;
    }

    [Header("Combo Settings")]
    [SerializeField] private AttackSettings attack1Settings = new AttackSettings { duration = 45 / 30, cancelableTime = 0.4f, hitboxStartTime = 0.2f, hitboxEndTime = 0.4f, rotationEndTime = 0.2f };
    [SerializeField] private AttackSettings attack2Settings = new AttackSettings { duration = 20 / 30, cancelableTime = 0.4f, hitboxStartTime = 0.2f, hitboxEndTime = 0.4f, rotationEndTime = 0.2f };
    [SerializeField] private AttackSettings attack3Settings = new AttackSettings { duration = 39 / 30, cancelableTime = 0.0f, hitboxStartTime = 0.3f, hitboxEndTime = 0.6f, rotationEndTime = 0.3f }; // 3段目はフィニッシュなのでコンボ猶予なし

    [Header("Hit Stop Settings")]
    [SerializeField] private float hitStopDuration = 0.15f;

    private int currentComboStep = 0;
    private int targetComboStep = 0;
    private bool isAttacking = false;
    private bool canCancel = false;
    private bool isHitStopping = false;

    public bool IsInvincible { get; set; }

    public void Initialize(CharacterAnimator animator, PlayerController playerController)
    {
        charAnim = animator;
        this.playerController = playerController;

        if (weaponHitbox != null)
        {
            weaponHitbox.Initialize(this); // 武器スクリプトの初期化
        }
    }

    public void PerformAttack()
    {
        if (targetComboStep >= 3) return;   // 3段目まで出し切っている（または目標が3）なら、これ以上の追加入力は無視（1段目への暴発を防ぐ）

        targetComboStep++;      // 入力があったら目標段数を増やす（先行入力のストック）

        if (!isAttacking)
        {
            ProceedToNextAttack();      // 攻撃中でなければ、即座に1段目を開始
        }
        else if (canCancel && currentComboStep < targetComboStep)
        {
            ProceedToNextAttack();      // すでにキャンセル可能タイミングに到達していれば、即座に次の段へ
        }
        // キャンセル不可のタイミングで押された場合は、targetComboStepが増えただけで待機
        // -> 後で AttackRoutine 側から自動的に発動する
    }

    private void ProceedToNextAttack()
    {
        currentComboStep++;
        ExecuteAttackStep(currentComboStep);
    }

    private void ExecuteAttackStep(int step)
    {
        isAttacking = true;
        canCancel = false;

        if (weaponHitbox != null) weaponHitbox.DisableHitbox();

        int animHash = 0;
        AttackSettings currentSettings = attack1Settings;

        switch (step)
        {
            case 1:
                animHash = charAnim.Attack1;
                currentSettings = attack1Settings;
                break;
            case 2:
                animHash = charAnim.Attack2;
                currentSettings = attack2Settings;
                break;
            case 3:
                animHash = charAnim.Attack3;
                currentSettings = attack3Settings;
                break;
        }

        charAnim.PlayState(animHash, 0.1f);     // アニメーション再生
        
        // 硬直管理のコルーチン開始
        StopAllCoroutines(); // 前のコンボのコルーチンをキャンセルして上書き
        StartCoroutine(AttackRoutine(currentSettings));
    }

    private IEnumerator AttackRoutine(AttackSettings settings)
    {
        float timer = 0f;
        bool hitboxActive = false;

        Vector3 attackDir = playerController.GetAttackDirection();

        while (timer < settings.duration)
        {
            if (!isHitStopping)
            {
                if (timer < settings.rotationEndTime)
                {
                    attackDir = playerController.GetAttackDirection();  // ロックオン中は敵が動くので常に最新の方向を取り直す
                    playerController.RotateTowards(attackDir, 15f);     // 15fは回転スピード
                }

                timer += Time.deltaTime;

                if (!hitboxActive && timer >= settings.hitboxStartTime && timer <= settings.hitboxEndTime)
                {
                    if (weaponHitbox != null) weaponHitbox.EnableHitbox();
                    hitboxActive = true;
                }
                else if (hitboxActive && timer > settings.hitboxEndTime)
                {
                    if (weaponHitbox != null) weaponHitbox.DisableHitbox();
                    hitboxActive = false;
                }

                if (!canCancel && timer >= settings.cancelableTime)
                {
                    canCancel = true;
                }

                if (canCancel && targetComboStep > currentComboStep)
                {
                    if (weaponHitbox != null) weaponHitbox.DisableHitbox();
                    ProceedToNextAttack();
                    yield break;
                }
            }
            yield return null;
        }
        
        if (weaponHitbox != null) weaponHitbox.DisableHitbox();
        ResetCombo();
    }

    public void TriggerHitStop()
    {
        if (!isHitStopping)
        {
            StartCoroutine(HitStopRoutine());
        }
    }

    public void SetInvincible(bool invincibility)
    {
        IsInvincible = invincibility;
    }

    private IEnumerator HitStopRoutine()
    {
        isHitStopping = true;
        if (charAnim.Animator != null)
        {
            charAnim.Animator.speed = 0f;
        }

        yield return new WaitForSeconds(hitStopDuration);

        if (charAnim.Animator != null)
        {
            charAnim.Animator.speed = 1f;
        }
        isHitStopping = false;
    }

    private void ResetCombo()
    {
        isAttacking = false;
        canCancel = false;
        currentComboStep = 0;
        targetComboStep = 0;

        playerController.ResetPriority(ActionPriority.Attack);
        playerController.ReturnToLocomotion();
    }

    // public void TakeDamage(float amount)
    // {
    //     if (playerController.TryExecuteAction(ActionPriority.Damage))
    //     {
    //         currentHealth -= amount;

    //         if (currentHealth <= 0)
    //         {
    //             playerController.TryExecuteAction(ActionPriority.Dead);
    //             Die();
    //         }
    //         else
    //         {
    //             charAnim.PlayDamage();
    //         }
    //     }
    //     else
    //     {
            
    //     }
    // }

    // private void CheckAttackCombo()
    // {
    //     //先行入力の有効期限チェック
    //     if (Time.time - lastAttackInputTime > inputBufferTime) return;

    //     if (charAnim == null) return;
    //     AnimatorStateInfo stateInfo = charAnim.GetCurrentAnimatorStateInfo(0);

    //     if (stateInfo.IsTag("Attack"))  //攻撃中なら
    //     {
    //         //攻撃中：アニメーションが指定位置まで進んでいれば次を発動
    //         if (stateInfo.normalizedTime >= attackCancelThreshold)
    //         {
    //             TriggerAttack();
    //         }
    //     }
    //     else if (movement.IsGrounded && !ability.IsUsingAbility)
    //     {
    //         TriggerAttack();
    //     }
    // }

    // private void TriggerAttack()
    // {
    //     if (playerController.TryExecuteAction(ActionPriority.Attack))
    //     {
    //         charAnim.PlayAttack();
    //         lastAttackInputTime = -100f;
    //     }
    // }

    // private void ResetAttackPriority()
    // {
    //     if (playerController.CurrentPriority == ActionPriority.Attack)
    //     {
    //         AnimatorStateInfo stateInfo = charAnim.GetCurrentAnimatorStateInfo(0);
    //         if (!stateInfo.IsTag("Attack"))
    //         {
    //             playerController.ResetPriority(ActionPriority.Attack);
    //         }
    //     }
    // }

    private void Die()
    {
        Debug.Log("YOU DIED");
    }
}
