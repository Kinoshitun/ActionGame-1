using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float dashSpeedMultiplier = 2f;
    [Tooltip("移動入力の反応速度。小さいほどキビキビ、大きいと慣性がつく")]
    [SerializeField] private float movementSmoothTime = 0.05f; 
    [Tooltip("回転速度。大きいほど速く向く")]
    [SerializeField] private float rotationSpeed = 15f; // Slerp用に値を調整

    [Header("Sura-Strike Settings")]//スラストライク設定
    [SerializeField] private float maxChargeTime = 1.5f;
    [SerializeField] private float attackSpeed = 25f;
    [SerializeField] private float attackDuration = 0.5f;
    [SerializeField] private float pushPower = 20;

    [Header("Visual")]
    [SerializeField] private Transform characterModel;
    [SerializeField] private Vector3 squashScale = new Vector3(1.5f, 0.5f, 1.5f);

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpForce = 1.5f; // 高さ(m)指定に変更
    [SerializeField] private float gravityValue = -15.0f; // キビキビさせるため重力強め
    [SerializeField] private float gravityMultiplier = 2.0f; // 落下時はさらに倍

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.2f;
    [SerializeField] private LayerMask groundMask;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    // Components
    private CharacterController controller;

    // State
    private Vector2 moveInput;
    private Vector3 currentVelocity; // SmoothDamp用
    private Vector3 moveDirectionVelocity; // SmoothDamp用
    private float verticalVelocity;
    private bool isGrounded;
    private bool isDashing;
    private float currentMaxSpeed;

    // Action State
    private bool isCharging;
    private float chargeTimer;
    private bool isAttacking;
    private float attackTimer;
    private Vector3 attackDirection;

    //デバッグ用変数
    private float rawDashInput;
    private bool isDashButtonPressed => rawDashInput > 0.5f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // カメラ自動取得
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        currentMaxSpeed = moveSpeed;
        if(characterModel == null) Debug.LogError("【重要】InspectorでCharacter Modelをセットしてください！ここが空だと変形しません！");
    }

    void Update()
    {
        HandleGround();
        HandleGravity();

        //状態遷移の管理
        HandleChargeLogic();

        if (isAttacking)
        {
            HandleAttackMovement();     //突撃中の動き
        }
        else if (isCharging)
        {
            HandleChargingMovement();   //溜め中の動き
        }
        else
        {
            HandleMovement();   //通常の動き
        }
    }

    void OnGUI()
    {
        GUI.color = Color.black;
        // 左上に、今のボタンの状態と、溜め時間を表示します
        GUI.Label(new Rect(20, 20, 300, 20), $"Button Pressed: {isDashButtonPressed} (Raw: {rawDashInput:F2})");
        GUI.Label(new Rect(20, 40, 300, 20), $"Charging: {isCharging}");
        GUI.Label(new Rect(20, 60, 300, 20), $"Timer: {chargeTimer:F2} / {maxChargeTime}");
        GUI.Label(new Rect(20, 80, 300, 20), $"Model Scale: {characterModel?.localScale}");
    }

    private void HandleChargeLogic()
    {
        //攻撃中は操作を受け付けない
        if (isAttacking) return;

        //ボタンが押されている間：チャージ
        if (isDashButtonPressed)
        {
            if (!isCharging)
            {
                //押し始めた瞬間
                isCharging = true;
                chargeTimer = 0f;
                currentVelocity = Vector3.zero;
            }

            //チャージ時間を加算
            chargeTimer += Time.deltaTime;
            if (chargeTimer > maxChargeTime) chargeTimer = maxChargeTime;
        }
        else //2. ボタンが離される & チャージ中 → 発射
        {
            if (isCharging)
            {
                isCharging = false;
                StartAttack();
            }
        }
    }

    private void HandleChargingMovement()
    {
        float chargePercent = Mathf.Clamp01(chargeTimer / maxChargeTime);

        //見た目の変形
        //チャージ量に応じて、元のサイズ(1,1,1)から潰れたサイズへ変形させる
        if (characterModel != null)
        {
            characterModel.localScale = Vector3.Lerp(Vector3.one, squashScale, chargePercent);
        }

        //チャージ中は移動できず、回転だけできるようにする
        if (moveInput.magnitude >= 0.1f)
        {
            Vector3 camForward = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 camRight = Vector3.Scale(cameraTransform.right, new Vector3(1, 0, 1)).normalized;
            Vector3 targetDir = (camForward * moveInput.y + camRight * moveInput.x).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(targetDir);
            characterModel.rotation = Quaternion.Slerp(characterModel.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        //重力だけ適用して動かない
        controller.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
    }

    //突撃中の処理
    private void HandleAttackMovement()
    {
        attackTimer -= Time.deltaTime;

        //時間切れで突撃停止
        if (attackTimer <= 0)
        {
            isAttacking = false;
            currentVelocity = Vector3.zero; //ピタッと止めるか、慣性を残すか
            if (characterModel != null) characterModel.localScale = Vector3.one;
            return;
        }

        //衝突速度の計算（後半少し減速させると自然）
        float currentSpeed = attackSpeed * (attackTimer / attackDuration);
        Vector3 velocity = attackDirection * currentSpeed;
        velocity.y = verticalVelocity; //重力は維持

        controller.Move(velocity * Time.deltaTime);

        //向きは進行方向に固定
        if (characterModel != null)
        {
            characterModel.rotation = Quaternion.LookRotation(attackDirection);
            //スケールを元に戻す（びよーんと戻る演出を入れたい場合はここでLerpする）
            characterModel.localScale = Vector3.one;
        }
    }

    private void StartAttack()
    {
        isAttacking = true;

        //溜め時間に応じて持続時間を変える（最大溜めで長く飛ぶ）
        float chargeRatio = Mathf.Clamp01(chargeTimer / maxChargeTime);
        //最低0.2秒、最大で設定値まで
        attackTimer = Mathf.Lerp(0.2f, attackDuration, chargeRatio);

        //向いている方向に飛ぶ
        if (characterModel != null) attackDirection = characterModel.forward;
        else attackDirection = transform.forward;

        if (characterModel != null) characterModel.localScale = Vector3.one;
    }

    private void HandleGround()
    {
        // 地面判定
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask);
        }
        else
        {
            isGrounded = controller.isGrounded;
        }

        // ▼【重要】接地時は重力をリセットする（これをしないと無限に加速して振動する）
        if (isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f; // 0ではなく-2にして、坂道などで浮かないように吸着させる
        }
    }

    private void HandleGravity()
    {
        // 天井に頭をぶつけたときの処理
        if ((controller.collisionFlags & CollisionFlags.Above) != 0)
        {
            if (verticalVelocity > 0)
            {
                verticalVelocity = 0f;
            }
        }

        // 落下中は重力を強くして、ジャンプの挙動をキビキビさせる（マリオ方式）
        float gravity = (verticalVelocity < 0 && !isGrounded) 
            ? gravityValue * gravityMultiplier 
            : gravityValue;

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void HandleMovement()
    {
        //スケールを安全にリセット
        if (characterModel != null) characterModel.localScale = Vector3.Lerp(characterModel.localScale, Vector3.one, Time.deltaTime * 10f);

        // 1. 入力ベクトルの計算
        Vector3 targetDirection = Vector3.zero;

        // 入力がある場合のみ方向計算
        if (moveInput.magnitude >= 0.1f)
        {
            // カメラの向きを基準に変換（Y軸の影響を消して正規化）
            Vector3 camForward = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 camRight = Vector3.Scale(cameraTransform.right, new Vector3(1, 0, 1)).normalized;
            targetDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;
        }

        //2.速度設定の更新
        //地面にいるときだけダッシュか歩きかを判断して、最高速度を切り替える。
        if (isGrounded)
        {
            if (isDashing) currentMaxSpeed = moveSpeed * dashSpeedMultiplier;
            else currentMaxSpeed = moveSpeed;
        }

        // 2. 移動速度のスムージング（慣性処理）
        // 入力がない時は (0,0,0) に向かって減速する
        float targetSpeedVal = (moveInput.magnitude < 0.1f) ? 0f : currentMaxSpeed;

        // キャラクターの進行方向ベクトル自体をスムーズに変化させる
        // これにより「急な方向転換」をした時に、一瞬だけ速度が落ちて弧を描くような自然な挙動になる
        currentVelocity = Vector3.SmoothDamp(currentVelocity, targetDirection * targetSpeedVal, ref moveDirectionVelocity, movementSmoothTime);

        // 3. 回転処理
        // 移動しようとしている方向（currentVelocity）があれば、そちらを向く
        if (currentVelocity.magnitude > 0.1f)
        {
            // 進行方向を向く回転を作成
            Quaternion targetRotation = Quaternion.LookRotation(currentVelocity.normalized);
            
            // Slerpで滑らかに回転させる（RotateTowardsより有機的）
            characterModel.rotation = Quaternion.Slerp(characterModel.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 4. 最終適用
        Vector3 finalMove = currentVelocity;
        finalMove.y = verticalVelocity; // 重力を合成

        controller.Move(finalMove * Time.deltaTime);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (isAttacking)
        {
            Rigidbody body = hit.collider.attachedRigidbody;

            //相手が物理挙動を持っていて、かつ静的でなければ
            if (body != null && !body.isKinematic)
            {
                Vector3 pushDir = hit.moveDirection;
                pushDir.y = 0.5f; //少し上に跳ね上げる

                body.AddForce(pushDir * pushPower, ForceMode.Impulse);
            }
        }
    }

    // Input System Events
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded && !isCharging)
        {
            // 高さ指定ジャンプの公式: v = sqrt(h * -2 * g)
            // jumpForceを「ジャンプする高さ(メートル)」として扱えるように変更
            verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravityValue);
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        rawDashInput = context.ReadValue<float>();
    }
}