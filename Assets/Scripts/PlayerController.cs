using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerAudio))]
public class PlayerController : MonoBehaviour
{
    #region --- Settings & Variables ---

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform playerModel;
    [SerializeField] private Slider energyBar;

    [Header("Ground Check Settings")]
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, 0.1f, 0);
    [SerializeField] private LayerMask groundMask;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;                      //移動速度
    [SerializeField] private float dashSpeedMultiplier = 1.5f;            //ダッシュの加速倍率
    [SerializeField] private float movementSmoothTime = 0.1f;          //入力に対する反応速度
    [SerializeField] private float rotationSpeed = 10f;                 //回転速度

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpForce = 1.5f;                    // 高さ(m)指定に変更
    [SerializeField] private float gravityValue = -15.0f;               // キビキビさせるため重力強め
    [SerializeField] private float gravityMultiplier = 2.0f;            // 落下時はさらに倍
    [SerializeField] private float landingPredictionHeight = 2.0f;

    [Header("Animation Settings")]
    [SerializeField] private float fallingDelay = 0.2f;

    [Header("Combat Settings")]
    [SerializeField] private float inputBufferTime = 0.4f;              //入力を覚えておく時間
    [SerializeField] private float attackCancelThreshold = 0.6f;        //最低6割は攻撃アニメーションを再生する

    [Header("Ability - Drain")]
    [SerializeField] private float drainRadius = 8f;
    [SerializeField] private float drainCooldown = 1.0f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private GameObject EnergyOrbPrefab;
    [SerializeField] private GameObject drainAreaEffect;
    [SerializeField] private float maxEnergy = 100f;

    [Header("Ability - Dash Strike")]
    [SerializeField] private float strikeEnergyCost = 50f;
    [SerializeField] private float strikeDashSpeed = 30f;
    [SerializeField] private float strikeDuration = 0.3f;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float knockbackForce = 20f;
    [SerializeField] private GameObject explosionVFXPrefab;

    // --- Private Variables ---
    private CharacterController controller;
    private Animator animator;
    private PlayerInput playerInput;
    private PlayerAudio playerAudio;

    // --- Movement State ---
    private Vector2 moveInput;
    private Vector3 currentVelocity;                                    // SmoothDamp用
    private Vector3 moveDirectionVelocity;                              // SmoothDamp用
    private float verticalVelocity;
    private float currentMaxSpeed;
    private bool isGrounded;
    private bool isDashing;
    private float lastGroundedTime;

    // --- Comabat State ---
    private float lastAttackInputTime = -1f;
    private float lastDrainTime;
    private float currentEnergy = 0f;
    private bool isDashStriking = false;

    #endregion

    #region --- Unity Lifecycle ---

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        playerInput = GetComponent<PlayerInput>();
        playerAudio = GetComponent<PlayerAudio>();

        //カメラ自動取得
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        //初期化
        if (energyBar != null) energyBar.value = currentEnergy;
    }

    void Update()
    {
        // 1. 接地判定
        GroundCheck();

        // 2. アクション中は通常の更新処理をスキップ
        if (isDashStriking) return;

        // 3. コンボ入力の監視
        CheckAttackCombo();

        // 4. 重力処理
        HandleGravity();

        // 5. 移動と回転
        HandleMovement();

        // 6. アニメーターへパラメータを送信
        UpdateAnimator();
    }

    void OnDrawGizmosSelected()
    {
        //ドレイン範囲の可視化
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, drainRadius);

        //着地予測線の可視化
        Gizmos.color = Color.red;
        Vector3 predictOrigin = transform. position + Vector3.up * 0.5f;
        Gizmos.DrawLine(predictOrigin, predictOrigin + Vector3.down * (landingPredictionHeight + 0.5f));

        //GroundCheckの可視化
        Gizmos.color = isGrounded ? new Color(1, 0, 0, 0.5f) : new Color(1, 1, 1, 0.5f);
        Gizmos.DrawSphere(transform.position + groundCheckOffset, groundCheckRadius);
    }

    #endregion

    #region --- Input Actions ---

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        isDashStriking = context.ReadValueAsButton();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded && !IsAttacking() && !isDashStriking)
        {
            verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravityValue);
            animator.SetTrigger("Jump");
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            lastAttackInputTime = Time.time; // 選考入力時間を記録
        }
    }

    public void OnDrain(InputAction.CallbackContext context)
    {
        if (context.performed && !isDashStriking) {
            PerformDrain();
        }
    }

    public void OnSpecialAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            AttemptDashStrike();
        }
    }

    #endregion

    #region --- Movement Logic ---

    private void HandleMovement()
    {
        bool isAttacking = IsAttacking();

        if (!isAttacking)
        {
            // --- A. 通常時の移動処理 ---

            //スケールの安定化（必要？）
            if (playerModel != null) playerModel.localScale = Vector3.Lerp(playerModel.localScale, Vector3.one, Time.deltaTime * 10f);

            //カメラ基準の移動方向算出
            Vector3 targetDirection = Vector3.zero;
            if (moveInput.magnitude >= 0.1f)
            {
                Vector3 camForward = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
                Vector3 camRight = Vector3.Scale(cameraTransform.right, new Vector3(1, 0, 1)).normalized;
                targetDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;
            }

            //速度設定
            currentMaxSpeed = (isGrounded && isDashStriking) ? moveSpeed * dashSpeedMultiplier : moveSpeed;

            //スムージング
            float targetSpeedVal = (moveInput.magnitude < 0.1f) ? 0f : currentMaxSpeed;
            float dynamicSmoothTime = (moveInput.magnitude > 0.1f) ? movementSmoothTime : 0.001f;

            currentVelocity = Vector3.SmoothDamp(currentVelocity, targetDirection * targetSpeedVal, ref moveDirectionVelocity, dynamicSmoothTime);

            //回転処理(移動中のみ)
            if (currentVelocity.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(currentVelocity.normalized);
                playerModel.rotation = Quaternion.Slerp(playerModel.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            // --- B. 攻撃中の処理（強制停止）---
            currentVelocity = Vector3.zero;
            moveDirectionVelocity = Vector3.zero;
        }

        // --- C. 最終適用(重力合成)--- 
        Vector3 finalMove = currentVelocity;
        finalMove.y = verticalVelocity; // 重力を合成

        controller.Move(finalMove * Time.deltaTime);
    }

    private void GroundCheck()
    {
        isGrounded = controller.isGrounded;
        isGrounded = Physics.CheckSphere(transform.position + groundCheckOffset, groundCheckRadius, groundMask);

        // 接地時は重力をリセットする（これをしないと無限に加速して振動する）
        if (isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f; // 0ではなく-2にして、坂道などで浮かないように吸着させる
        }
    }

    private void HandleGravity()
    {
        verticalVelocity += gravityValue * Time.deltaTime;
    }

    private bool IsAttacking()
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack");
    }

    #endregion

    #region --- Animation Logic ---

    private void UpdateAnimator()
    {
        if (animator == null) return;

        //速度(水平のみ)
        float horizontalSpeed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
        float currentSpeed = currentVelocity.magnitude;
        animator.SetFloat("Speed", horizontalSpeed, 0.1f, Time.deltaTime);

        //接地判定
        if (isGrounded) lastGroundedTime = Time.time;
        bool isGroundedForAnim = isGrounded || (Time.time - lastGroundedTime < fallingDelay);
        animator.SetBool("IsGrounded", isGroundedForAnim);

        bool isCloseToGround = false;
        if (verticalVelocity < -0.1f && !isGrounded)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
            float castRadius = controller.radius * 0.9f;
            float castDist = landingPredictionHeight + 0.5f;

            if (Physics.SphereCast(rayOrigin, castRadius, Vector3.down, out RaycastHit hit, castDist, groundMask) && verticalVelocity < 0)
            {
                isCloseToGround = true;
            }
        }
        animator.SetBool("IsCloseToGround", isCloseToGround);
    }

    #endregion

    #region --- Combat Logic (Combo) ---

    private void CheckAttackCombo()
    {
        //選考入力の有効期限チェック
        if (Time.time - lastAttackInputTime > inputBufferTime) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsTag("Attack"))  //攻撃アニメーションが再生中なら
        {
            //攻撃中：アニメーションが指定位置まで進んでいれば次を発動
            if (stateInfo.normalizedTime >= attackCancelThreshold)
            {
                animator.SetTrigger("Attack");
                lastAttackInputTime = -1f;  //消費
            }
        }
        else if (isGrounded && !isDashStriking)
        {
            //通常時：即発動
            animator.SetTrigger("Attack");
            lastAttackInputTime = -1f;
        }
    }

    #endregion

    #region --- Ability: Drain ---

    private void PerformDrain()
    {
        if (Time.time - lastDrainTime < drainCooldown) return;
        lastDrainTime = Time.time;

        // エフェクト生成
        if (drainAreaEffect != null)
        {
            GameObject effect = Instantiate(drainAreaEffect, transform.position, Quaternion.identity);
            float diameter = drainRadius * 2;
            effect.transform.localScale = new Vector3(drainRadius * 2, effect.transform.localScale.y, diameter);
            Destroy(effect, 0.5f);
        }

        //animator.SetTrigger("Drain");

        Collider[] enemies = Physics.OverlapSphere(transform.position, drainRadius, enemyLayer);
        foreach (Collider col in enemies)
        {
            EnemyDummy enemy = col.GetComponent<EnemyDummy>();
            if (enemy != null)
            {
                enemy.OnDrain();

                if (EnergyOrbPrefab != null)
                {
                    GameObject orb = Instantiate(EnergyOrbPrefab, col.transform.position + Vector3.up, Quaternion.identity);
                    EnergyOrb orbScript = orb.GetComponent<EnergyOrb>();
                    if (orbScript != null) orbScript.Initialize(this.transform);
                }
            }
        }
    }

    public void AddEnergy(float amount)
    {
        currentEnergy += amount;
        if (currentEnergy > maxEnergy) currentEnergy = maxEnergy;
        if (currentEnergy < 0) currentEnergy = 0;

        if (energyBar != null) energyBar.value = currentEnergy;
    }

    #endregion

    #region --- Ability DashStrike ---

    private void AttemptDashStrike()
    {
        if (currentEnergy >= strikeEnergyCost && isGrounded && !isDashStriking && !IsAttacking())
        {
            StartCoroutine(DashStrikeRoutine());
        }
        else
        {
            Debug.Log("今は技を出せない！");
        }
    }

    private IEnumerator DashStrikeRoutine()
    {
        isDashStriking = true;
        AddEnergy(-strikeEnergyCost);

        animator.SetTrigger("DashStrike");

        float startTime = Time.time;

        Vector3 dashDirection = (playerModel != null) ? playerModel.forward : transform.forward;

        RaycastHit hitInfo = new RaycastHit();  
        bool hitSomething = false;

        //突進フェーズ
        while (Time.time < startTime + strikeDuration)
        {
            //高速移動
            controller.Move(dashDirection * strikeDashSpeed * Time.deltaTime);
            //衝突判定
            if (Physics.SphereCast(playerModel.position + Vector3.up, 0.5f, dashDirection, out hitInfo, 1.0f, enemyLayer))
            {
                hitSomething = true;
                break;  //衝突したら即爆発へ
            }
            yield return null;
        }

        //爆発フェーズ
        currentVelocity = Vector3.zero; //停止

        //敵にあたった時だけヒットストップ演出
        if (hitSomething)
        {
            Time.timeScale = 0.1f;
            yield return new WaitForSecondsRealtime(0.1f);
            Time.timeScale = 1.0f;
        }
        
        Vector3 explosionPosition = hitSomething ? hitInfo.point : transform.position + playerModel.forward * 2f;

        //エフェクト出す
        if (explosionVFXPrefab != null)
        {
            GameObject vfx = Instantiate(explosionVFXPrefab, explosionPosition, Quaternion.identity);
            Destroy(vfx, 2.0f);
        }

        //吹き飛ばし
        Collider[] hitColliders = Physics.OverlapSphere(explosionPosition, explosionRadius, enemyLayer);
        foreach (Collider col in hitColliders)
        {
            Rigidbody rb = col.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 knockbackDir = (col.transform.position - explosionPosition).normalized + Vector3.up * 0.5f;
                rb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
            }
        }

        //硬直時間
        yield return new WaitForSeconds(0.2f);

        isDashStriking = false;
    }

    #endregion
}