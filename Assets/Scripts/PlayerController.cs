using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float dashSpeedMultiplier = 2f;
    [SerializeField] private float rotationSmoothTime = 0.12f; //回転のスムーズさ

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravityValue = -9.8f;
    [SerializeField] private float gravityMultiplier = 2.0f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.2f;
    [SerializeField] private LayerMask groundMask;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    //Internal State
    private CharacterController characterController;
    private Vector2 moveInput;
    private Vector3 currentVelocity;
    private float verticalVelocity;
    private bool isGrounded;
    private bool isDashing;

    //Rotation smoothing variables
    private float targetRotation;
    private float rotationVelocity;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.Log("CharacterControllerコンポーネントが見つかりません");
            enabled = false;
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        HandleGroundCheck();
        HandleGravity();
        HandleMovenment();
    }

    private void HandleGroundCheck()
    {
        //SphereCheckが設定されていれば優先、なければCCの機能を使う
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }
        else
        {
            isGrounded = characterController.isGrounded;
        }
    }

    private void HandleGravity()
    {
        //重力の適用
        //落下中（verticalVelocity < 0）は重力を強くするとゲームの手触りがよくなることが多い
        float currentGravity = (verticalVelocity < 0 && !isGrounded)
            ? gravityValue * gravityMultiplier
            : gravityValue;

        verticalVelocity += currentGravity * Time.deltaTime;
    }

    private void HandleMovenment()
    {
        //入力がない場合は処理をスキップ（重力移動のみ適用）
        if (moveInput.magnitude < 0.1f)
        {
            characterController.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
            return;
        }

        

        //1.カメラの向きを基準に入力方向を変換
        //Atan2で入力角度を計算し、カメラのY軸回転を加算する
        float targetAngle = Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;

        //2.プレイヤーの回転（スムージング込み）
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);

        //3.移動方向の算出（回転した方向に進む）
        Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

        //4.スピード計算
        float targetSpeed = isDashing ? moveSpeed * dashSpeedMultiplier : moveSpeed;

        //5.最終的な移動ベクトルの適用（水平移動＋垂直移動）
        Vector3 finalMove = moveDirection.normalized * targetSpeed;
        finalMove.y = verticalVelocity;

        characterController.Move(finalMove * Time.deltaTime);
    }

    //Input system events ------------------------------------

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpForce * -2f * gravityValue);
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed) isDashing = true;
        if (context.canceled) isDashing = false;
    }
}
