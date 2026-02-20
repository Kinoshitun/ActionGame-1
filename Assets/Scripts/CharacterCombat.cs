using UnityEngine;
using System.Collections;

public class CharacterCombat : MonoBehaviour
{
    private CharacterAnimator charAnim;
    private PlayerController playerController;

    // [SerializeField] private float maxHealth = 100f;
    // private float currentHealth;

    [System.Serializable]
    public struct AttackSettings
    {
        public float duration;
        public float cancelableTime;
    }

    [Header("Combo Settings")]
    [SerializeField] private AttackSettings attack1Settings = new AttackSettings { duration = 45 / 30, cancelableTime = 0.4f };
    [SerializeField] private AttackSettings attack2Settings = new AttackSettings { duration = 20 / 30, cancelableTime = 0.4f };
    [SerializeField] private AttackSettings attack3Settings = new AttackSettings { duration = 39 / 30, cancelableTime = 0.0f }; // 3段目はフィニッシュなのでコンボ猶予なし

    private int currentComboStep = 0;
    private int targetComboStep = 0;
    private bool isAttacking = false;
    private bool canCancel = false;

    public bool IsInvincible { get; set; }
    // public bool IsAttacking {get; private set; }

    void Awake()
    {
        // movement = GetComponent<CharacterMovement>();
        // ability = GetComponent<CharacterAbility>();
        // currentHealth = maxHealth;
    }

    public void Initialize(CharacterAnimator animator, PlayerController playerController)
    {
        this.charAnim = animator;
        this.playerController = playerController;
    }

    void Update()
    {
        // CheckAttackCombo();
        // ResetAttackPriority();
    }

    public void PerformAttack()
    {
        // ★修正2：3段目まで出し切っている（または目標が3）なら、これ以上の追加入力は無視（1段目への暴発を防ぐ）
        if (targetComboStep >= 3) return;

        // ★修正1：入力があったら目標段数を増やす（先行入力のストック）
        targetComboStep++;

        // 攻撃中でなければ、即座に1段目を開始
        if (!isAttacking)
        {
            ProceedToNextAttack();
        }
        // すでにキャンセル可能タイミングに到達していれば、即座に次の段へ
        else if (canCancel && currentComboStep < targetComboStep)
        {
            ProceedToNextAttack();
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

        int animHash = 0;
        float duration = 0f;
        float cancelTime = 0f;

        switch (step)
        {
            case 1:
                animHash = charAnim.Attack1;
                duration = attack1Settings.duration;
                cancelTime = attack1Settings.cancelableTime;
                break;
            case 2:
                animHash = charAnim.Attack2;
                duration = attack2Settings.duration;
                cancelTime = attack2Settings.cancelableTime;
                break;
            case 3:
                animHash = charAnim.Attack3;
                duration = attack3Settings.duration;
                cancelTime = attack3Settings.cancelableTime;
                break;
        }

        // アニメーション再生
        charAnim.PlayState(animHash, 0.1f);
        
        // 硬直管理のコルーチン開始
        StopAllCoroutines(); // 前のコンボのコルーチンをキャンセルして上書き
        StartCoroutine(AttackRoutine(duration, cancelTime));
    }

    private IEnumerator AttackRoutine(float totalDuration, float cancelTime)
    {
        float timer = 0f;

        while (timer < cancelTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        canCancel = true;

        if (targetComboStep > currentComboStep)
        {
            ProceedToNextAttack();
            yield break;
        }

        while (timer < totalDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        ResetCombo();
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

    public void SetInvincible(bool invincibility)
    {
        IsInvincible = invincibility;
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
