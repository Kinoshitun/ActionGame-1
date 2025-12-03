using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Visual")]
    [SerializeField] private Transform characterModel;

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

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // カメラ自動取得
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        currentMaxSpeed = moveSpeed;
    }

    void Update()
    {
        HandleGround();
        HandleGravity();
        HandleMovement();
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

    // Input System Events
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded)
        {
            // 高さ指定ジャンプの公式: v = sqrt(h * -2 * g)
            // jumpForceを「ジャンプする高さ(メートル)」として扱えるように変更
            verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravityValue);
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed) isDashing = true;
        if (context.canceled) isDashing = false;
    }
}