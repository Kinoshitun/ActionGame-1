using UnityEngine;

/// <summary>
/// キャラクターの基本的な物理挙動を管理する。
/// 水平移動、ジャンプ、落下、着地、RootMotionなど
/// </summary>

[RequireComponent(typeof(CharacterController))] //キャラクターを動かすために必要
public class CharacterMovement : MonoBehaviour
{
    [Header("Ground Check")]
    [SerializeField] private float groundCheckRadius = 0.4f;
    [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, 0.15f, 0);
    [SerializeField] private LayerMask groundMask;

    [Header("Jump")]
    public float jumpHeight = 2.0f;
    public float jumpDuration = 0.4f;
    public float maxFallSpeed = 20.0f;
    public float airSmoothTime = 0.2f;
    public float minAirSpeed = 3.0f;

    [Header("HardLanding")]
    [SerializeField] private float hardLandingThreshold = 6.0f;
    public float FallDistance => currentFallDistance;   //外部参照用

    [Header("Root Motion")]
    [SerializeField] private bool useRootMotion = true;

    //外部参照用
    public CharacterController Controller { get; private set; }
    public bool IsGrounded { get; private set; }

    public float ActualHorizontalSpeed => new Vector3(calculatedVelocity.x, 0, calculatedVelocity.z).magnitude;   //実際の移動速度
    public float ActualVerticalSpeed => calculatedVelocity.y;

    //内部参照用
    private CharacterAnimator charAnim;

    private Vector3 inputVelocity;
    private Vector3 smoothDampVelocity;
    private float verticalVelocity;

    private Vector3 jumpMomentum;
    private float gravity;
    private float initialJumpVelocity;

    private float currentFallDistance;
    private float peakHeight;

    private Vector3 calculatedVelocity;

    // マジックナンバーの定数化
    private const float GROUND_STICKING_VELOCITY = -5.0f;
    private const float DIRECTION_THRESHOLD = 0.1f;
    private const float VELOCITY_LERP_SPEED = 20.0f;

    void Awake()
    {
        Controller = GetComponent<CharacterController>();   //キャラクターコントローラーの取得
        SetupGravity();                                  //重力の初期設定
        CheckGround();
    }

    void Update()
    {
        CheckGround();              //接地情報の更新
        HandleGravity();            //重力を常にかけておく

        Vector3 finalMove = inputVelocity + new Vector3(0, verticalVelocity, 0);

        Controller.Move(finalMove * Time.deltaTime);

        CalculateActualVelocity();
        CalculateFallDistance();    //落下距離情報の更新
    }

    public void Initialize(CharacterAnimator animator)
    {
        this.charAnim = animator;
    }

    public void GroundInputMove(Vector3 targetDirection, float targetSpeed, float smoothTime, float rotationSpeed)
    {
        Vector3 targetVelocity = targetDirection * targetSpeed;

        inputVelocity = Vector3.SmoothDamp(
            inputVelocity,
            targetVelocity,
            ref smoothDampVelocity,
            smoothTime
        );

        ApplyRotation(targetDirection, rotationSpeed);
    }

    public void AirInputMove(Vector3 targetDirection, float airControlSpeed, float rotationSpeed)
    {
        Vector3 controlVelocity = targetDirection * airControlSpeed;
        Vector3 finalVelocity = jumpMomentum + controlVelocity;
        float maxAirSpeed = Mathf.Max(jumpMomentum.magnitude, airControlSpeed);

        if (finalVelocity.magnitude > maxAirSpeed)
        {
            finalVelocity = finalVelocity.normalized * maxAirSpeed;
        }
        inputVelocity = finalVelocity;
        ApplyRotation(targetDirection, rotationSpeed);
    }

    //外部からの命令用
    public void ActJump()
    {
        verticalVelocity = initialJumpVelocity; //垂直成分にジャンプの初速を与える
        Vector3 currentVel = Controller.velocity;
        currentVel.y = 0;
        jumpMomentum = currentVel;
        inputVelocity = jumpMomentum;
        smoothDampVelocity = Vector3.zero;
        IsGrounded = false;
        currentFallDistance = 0;
    }

    public void ForceMove(Vector3 motion)
    {
        Controller.Move(motion);
        StopImmediately();
        CalculateActualVelocity();
    }

    public void SyncVelocity()
    {
        Vector3 realVelocity = Controller.velocity;
        realVelocity.y = 0;

        inputVelocity = realVelocity;
        smoothDampVelocity = Vector3.zero;
    }

    public void StopImmediately()
    {
        inputVelocity = Vector3.zero;
        smoothDampVelocity = Vector3.zero;
        jumpMomentum = Vector3.zero;
    }

    public void ApplyRootMotion(Vector3 deltaPosition)
    {
        if (IsGrounded)
        {
            deltaPosition.y = 0;
            Controller.Move(deltaPosition);
        }
    }

    public void ApplyRotation(Vector3 direction, float speed)
    {
        if (direction.magnitude > DIRECTION_THRESHOLD)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                speed * Time.deltaTime
            );
        }
    }

    public bool CheckHardLanding()
    {
        return currentFallDistance > hardLandingThreshold;
    }

    private void OnAnimatorMove()
    {
        if (IsGrounded && useRootMotion)
        {
            if (charAnim == null) return;

            if (inputVelocity.magnitude < 0.1f)
            {
                Vector3 deltaPos = charAnim.Animator.deltaPosition;
                deltaPos.y = 0;
                Controller.Move(deltaPos);
            }
        }
    }

    private void SetupGravity()
    {
        gravity = - (2 * jumpHeight) / Mathf.Pow(jumpDuration, 2);
        initialJumpVelocity = Mathf.Abs(gravity) * jumpDuration;
    }

    private void HandleGravity()
    {
        if (IsGrounded && verticalVelocity < 0)
        {
            verticalVelocity = GROUND_STICKING_VELOCITY; //接地吸着
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
            if (verticalVelocity < -maxFallSpeed) verticalVelocity = -maxFallSpeed;
        }
    }

    private void CheckGround()
    {
        bool isNowGrounded = Physics.CheckSphere(transform.position + groundCheckOffset, groundCheckRadius, groundMask);

        // ★追加：接地から空中に切り替わった瞬間にリセットする
        if (IsGrounded && !isNowGrounded)
        {
            currentFallDistance = 0f;
            peakHeight = transform.position.y;
        }

        IsGrounded = isNowGrounded;
    }

    //Updateで時間ごとに落下距離を計算する
    private void CalculateFallDistance()
    {
        if (!IsGrounded)
        {   //地面から離れ、最高高度よりも高度が高くなったら、最高高度を更新する
            if (transform.position.y > peakHeight) peakHeight = transform.position.y;
            //地面から離れ、高度が低くなったら、最高高度から現在の高度を引いて落下距離を計算する
            currentFallDistance = Mathf.Max(0, peakHeight - transform.position.y);
        }
    }

    private void CalculateActualVelocity()
    {
        // 複雑な位置計算を廃止し、CharacterControllerの正確な速度を直接もらう
        Vector3 rawVelocity = Controller.velocity;
        
        // スムージング (アニメーションの急な変化を防ぐ)
        calculatedVelocity = Vector3.Lerp(calculatedVelocity, rawVelocity, Time.deltaTime * VELOCITY_LERP_SPEED);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = IsGrounded ? Color.red : Color.white;
        Gizmos.DrawSphere(transform.position + groundCheckOffset, groundCheckRadius);
    }
}
