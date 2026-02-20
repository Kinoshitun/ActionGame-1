using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ActionPriority  //数字が小さい行動は数字が大きい行動でキャンセル可能
{
    Idle = 0,
    Move = 5,       //移動やジャンプ等
    Attack = 10,    //攻撃アクション
    Ability = 20,   //スキル
    Dodge = 30,     //回避アクション
    Recovery = 40,  //硬直時間
    Damage = 50,    //被弾
    Dead = 100      //死亡
}

public enum PlayerActionState
{
    Locomotion,
    Airborne,
    Dodge,
    HardLanding,
    Stunned,
    Attack
}

/// <summary>
/// Input Actions から入力を受け取り、他クラスに指示を出す
/// Playerの本質となるコンポーネント
/// </summary>

public class PlayerController : MonoBehaviour
{
    [Header("Sub Components")]
    [SerializeField] private CharacterAnimator charAnim;
    [SerializeField] private CharacterMovement movement;
    [SerializeField] private CharacterCombat combat;
    [SerializeField] private CharacterAbility ability;

    [System.Serializable]
    public struct DodgeSettings
    {
        public string name;           // わかりやすくするための名前 (Backstepなど)
        public float distance;        // 移動距離
        public float duration;        // 全体の時間
        [Range(0f, 1f)] public float moveStartRatio; // 移動開始タイミング (0.0~1.0)
        [Range(0f, 1f)] public float moveEndRatio;   // 移動終了タイミング (0.0~1.0)
        public int frameCount;
        
        // 無敵やアニメーション情報もここに含める
        public bool hasInvincibility;
    }

    [System.Serializable]
    public struct HardLandingSettings
    {
        public float duration;
        public float inputAcceptanceWindow;
        public int frameCount;
    }

    [System.Serializable]
    public struct MovementSettings
    {
        [Header("Speed")]
        public float walkSpeed;
        public float runSpeed;
        public float airSpeed;

        [Header("Control")]
        public float groundSmoothTime;
        public float rotationSpeed;
    }

    [Header("Movement Config")]
    [SerializeField] private MovementSettings moveSettings;
    private Vector2 moveInput;
    private bool sprintInput;

    [Header("Dodge Config")]
    [SerializeField] private DodgeSettings[] dodgeSettings;
    [SerializeField] private float dodgeCooldown = 0.2f;
    [SerializeField] private float dodgeInputBufferTime = 0.4f;
    private float lastDodgeEndTime = -10f;
    private bool dodgeInput;
    private float dodgeInputTime = -10f;    //ボタンを押した時刻

    [Header("Landing Config")]
    [SerializeField] private HardLandingSettings landingSettings;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    private Vector3 targetDirection = Vector3.zero;         //入力の処理データ
    public ActionPriority CurrentPriority { get; private set; } = ActionPriority.Idle;
    private PlayerActionState currentState;                 //現在のstate

    private bool guardInput;
    public bool IsGuarding { get; private set; }

    // public bool IsInvincible;

    // --- Unity Lifecycle ---
    void Awake()
    {
        if (movement == null) movement = GetComponent<CharacterMovement>();
        if (charAnim == null) charAnim = GetComponent<CharacterAnimator>();
        if (combat == null) combat = GetComponent<CharacterCombat>();
        if (ability == null) ability = GetComponent<CharacterAbility>();
        
        if (movement != null) movement.Initialize(charAnim);
        if (combat != null) combat.Initialize(charAnim, this);

        if (cameraTransform == null && Camera.main != null) 
            cameraTransform = Camera.main.transform;
    }

    void Start()
    {
        ChangeState(PlayerActionState.Locomotion);
    }

    void Update()   //入力を各コンポーネントに送信、動きとアニメーションを
    {
        CheckAirborneState();
        UpdateGuardState();

        // ステートを更新する処理、今自分が何をしているのか思い出す
        switch (currentState)
        {
            case PlayerActionState.Locomotion:
                HandleGroundMovement();
                break;
            case PlayerActionState.Airborne:
                HandleAirborneMovement();
                break;
            case PlayerActionState.Dodge:
            case PlayerActionState.Stunned:
            case PlayerActionState.HardLanding:
            case PlayerActionState.Attack:
                movement.StopImmediately();
                break;
        }

        ProcessInput();

        if (charAnim != null && movement != null)
        {
            float animVerticalSpeed = movement.ActualVerticalSpeed;
            float animHorizontalSpeed = movement.ActualHorizontalSpeed;   //入力方向に投射した値をアニメーションのスピードとする
            if (moveInput.magnitude < 0.1f) animHorizontalSpeed = 0f;
            if (movement.IsGrounded) animVerticalSpeed = 0f;
            charAnim.UpdateMovement(animHorizontalSpeed, animVerticalSpeed, movement.IsGrounded);
        }
    }

    private void CheckAirborneState()
    {
        if (currentState == PlayerActionState.Dodge || currentState == PlayerActionState.Stunned) return;

        if (currentState == PlayerActionState.Locomotion && !movement.IsGrounded)
        {
            ChangeState(PlayerActionState.Airborne);
        }
        else if (currentState == PlayerActionState.Airborne && movement.IsGrounded)
        {
            if (movement.ActualVerticalSpeed > 0.1f) return;

            if (movement.CheckHardLanding())
            {
                ChangeState(PlayerActionState.HardLanding);
                StartCoroutine(HardLandingRoutine());
            }
            else
            {
                ChangeState(PlayerActionState.Locomotion);
            }
        }
    }

    private void ChangeState(PlayerActionState newState)    //アニメーターのステートを更新
    {
        if (currentState == newState) return;
        currentState = newState;

        switch (currentState)
        {
            case PlayerActionState.Locomotion:
                charAnim.PlayState(charAnim.Locomotion, 0.25f);
                break;
            case PlayerActionState.Airborne:
                charAnim.PlayState(charAnim.Airborne, 0.1f);
                break;
            case PlayerActionState.HardLanding:
                break;
            case PlayerActionState.Dodge:
            case PlayerActionState.Attack:
                break;
        }
    }

    private void UpdateGuardState()
    {
        if (currentState == PlayerActionState.Locomotion && CurrentPriority < ActionPriority.Attack)
        {
            IsGuarding = guardInput;
        }
        else
        {
            IsGuarding = false;
        }

        charAnim.SetGuarding(IsGuarding);

        bool isGuardMoving = IsGuarding && (moveInput.magnitude > 0.1f) && !sprintInput;
        charAnim.SetGuardMoving(isGuardMoving);
    }

    public void HandleGroundMovement()
    {
        if (CurrentPriority >= ActionPriority.Attack) return;

        if (dodgeInput)
        {
            // クールダウンチェックなど
            if (Time.time >= lastDodgeEndTime + dodgeCooldown)
            {
                int dodgeType = 0;
                float inputMagnitude = moveInput.magnitude;
                if (sprintInput && inputMagnitude > 0.1f)
                {
                    dodgeType = 2;
                }
                else if (inputMagnitude > 0.1f)
                {
                    dodgeType = 1;
                }
                PerformDodge(dodgeType);
            }
            
            // 処理したのでフラグを下ろす
            dodgeInput = false; 
            return; // Dodgeを実行したら移動処理はしない
        }
        
        float targetSpeed = 0f;
        if (moveInput.magnitude > 0.1f)
        {
            if (IsGuarding)
            {
                targetSpeed = moveSettings.walkSpeed * 0.6f;
            }
            else
            {
                targetSpeed = sprintInput ? moveSettings.runSpeed : moveSettings.walkSpeed;
            }
        }

        movement.GroundInputMove(targetDirection, targetSpeed, moveSettings.groundSmoothTime, moveSettings.rotationSpeed);
    }

    public void HandleAirborneMovement()
    {
        float controlSpeed = (moveInput.magnitude > 0.1f) ? moveSettings.airSpeed : 0f;
        movement.AirInputMove(targetDirection, controlSpeed, moveSettings.rotationSpeed);
    }

    public void ReturnToLocomotion()
    {
        ChangeState(PlayerActionState.Locomotion);
    }

    //--- Helper Function ---

    private void ProcessInput()
    {
        //カメラ基準の移動方向算出
        if (moveInput.magnitude >= 0.1f)
        {
            Vector3 camForward = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
            Vector3 camRight = Vector3.Scale(cameraTransform.right, new Vector3(1, 0, 1)).normalized;
            targetDirection = (camForward * moveInput.y + camRight * moveInput.x).normalized;
        }
        else
        {
            sprintInput = false;
        }
    }

    // ---　優先度管理 ---

    public bool TryExecuteAction(ActionPriority requestPriority)    //状態遷移していいかどうかチェックする関数
    {
        // 例外1: 回避中の回避は禁止（連打防止）
        if (requestPriority == ActionPriority.Dodge && CurrentPriority == ActionPriority.Dodge) return false;
        
        // 例外2: 空中では回避できない（仕様による）
        if (requestPriority == ActionPriority.Dodge && !movement.IsGrounded) return false;

        // 例外3: 無敵中はダメージ無効
        if (requestPriority == ActionPriority.Damage && combat.IsInvincible) return false;

        // 基本ルール: 優先度が高いなら許可
        if (requestPriority >= CurrentPriority)
        {
            CurrentPriority = requestPriority;
            return true;
        }
        return false;
    }

    public void ResetPriority(ActionPriority myPriority)
    {
        if (CurrentPriority == myPriority)
        {
            CurrentPriority = ActionPriority.Idle;
        }
    }

    //--- Input Actions ---

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();   //入力値を保存
        Debug.Log($"moveInput: {moveInput.magnitude}"); //1が出力される
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            sprintInput = !sprintInput;
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && currentState == PlayerActionState.Locomotion)
        {
            PerformJump();
        }
    }

    public void OnDodge(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            dodgeInput = true;
            dodgeInputTime = Time.time;
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (movement.IsGrounded && TryExecuteAction(ActionPriority.Attack))
            {
                ChangeState(PlayerActionState.Attack);
                combat.PerformAttack();
            }
        }
    }

    public void OnGuard(InputAction.CallbackContext context)
    {
        if (context.performed) guardInput = true;
        if (context.canceled) guardInput = false;
    }

    // --- Action Execution ---

    private void PerformDodge(int dodgeType)    //Dodgeの命令を下す
    {
        if (!TryExecuteAction(ActionPriority.Dodge)) return;
        ChangeState(PlayerActionState.Dodge);                       //ステート更新  

        if (dodgeType >= dodgeSettings.Length) dodgeType = 0;
        DodgeSettings currentSettings = dodgeSettings[dodgeType];   //データ取得

        Vector3 dodgeDir;
        if (dodgeType == 0) {
            dodgeDir = -transform.forward;
        }
        else
        {
            dodgeDir = (moveInput.magnitude > 0.1f) ? targetDirection : transform.forward;
        }

        int animHash = (dodgeType == 2) ? charAnim.Dive :
                       (dodgeType == 1) ? charAnim.Roll : charAnim.Backstep;

        float originalDuration = currentSettings.frameCount / 30f;
        float speedMultiplier = originalDuration / currentSettings.duration;

        charAnim.Animator.SetFloat("ActionSpeed", speedMultiplier);
        charAnim.PlayState(animHash, 0.1f);

        StartCoroutine(DodgeRoutine(dodgeDir, currentSettings));
    }

    private void PerformJump()
    {
        movement.ActJump();
        ChangeState(PlayerActionState.Airborne);
    }

    // public void OnAttack(InputAction.CallbackContext context)
    // {
    //     if (context.performed)
    //     {
    //         combat.Attack();
    //     }
    // }

    // public void OnDrain(InputAction.CallbackContext context)
    // {
    //     if (context.performed)
    //     {
    //         ability.UseDrain();
    //     }
    // }

    private IEnumerator DodgeRoutine(Vector3 direction, DodgeSettings settings)  //Dodgeのコルーチン
    {
        //Player移動
        float timer = 0f;
        float activeDuration = settings.duration * (settings.moveEndRatio - settings.moveStartRatio);
        float speed = (activeDuration > 0) ? (settings.distance / activeDuration) : 0f;

        if (settings.hasInvincibility) combat.IsInvincible = true;

        movement.StopImmediately();

        while (timer < settings.duration)
        {
            float ratio = timer / settings.duration;
            bool isActive = ratio >= settings.moveStartRatio && ratio <= settings.moveEndRatio;

            if (isActive)
            {
                movement.ForceMove(direction * speed * Time.deltaTime);
            }
            else
            {
                if (settings.hasInvincibility) combat.SetInvincible(false);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        lastDodgeEndTime = Time.time;
        if (settings.hasInvincibility) combat.SetInvincible(false);

        ResetPriority(ActionPriority.Dodge);
        ChangeState(PlayerActionState.Locomotion);
    }

    private IEnumerator HardLandingRoutine()
    {
        movement.StopImmediately();

        if (dodgeInput)
        {
            float timeSinceInput = Time.time - dodgeInputTime;
            if (timeSinceInput > dodgeInputBufferTime)
            {
                dodgeInput = false;
            }
        }

        if (dodgeInput)
        {
            dodgeInput = false;
            PerformDodge(1);
            yield break;
        }

        float originalDuration = landingSettings.frameCount / 30f;
        float speedMultiplier = originalDuration / landingSettings.duration;

        // 速度をセットしてから再生
        charAnim.Animator.SetFloat("ActionSpeed", speedMultiplier);
        charAnim.PlayState(charAnim.HardLanding, 0.1f);

        float hardLandingDuration = landingSettings.duration;
        float timer = 0f;

        while (timer < hardLandingDuration)
        {
            if (timer <= landingSettings.inputAcceptanceWindow)
            {
                if (dodgeInput)
                {
                    dodgeInput = false;
                    PerformDodge(1);
                    yield break;
                }
            }
            else
            {
                dodgeInput = false;
            }

            timer += Time.deltaTime;
            yield return null;
        }
        dodgeInput = false;

        // 4. 復帰
        ChangeState(PlayerActionState.Locomotion);
        charAnim.Animator.SetFloat("ActionSpeed", 1.0f);
    }

    private void OnDrawGizmosSelected()
    {
        // 実行中以外や、参照がない時はエラーになるので帰る
        if (!Application.isPlaying || movement == null) return;
        // 開始位置（地面に埋まらないように少し浮かせる）
        Vector3 startPos = transform.position + Vector3.up * 1.0f;

        // ------------------------------
        // 1. 緑色：入力した方向 (Target Direction)
        // ------------------------------
        Gizmos.color = Color.green;
        // 入力がある時だけ描画
        if (moveInput.magnitude > 0.1f)
        {
            // 2mくらいの長さで線を引く
            Vector3 inputLine = targetDirection * 2.0f;
            Gizmos.DrawRay(startPos, inputLine);
            
            // 先端に球を描いてわかりやすくする
            Gizmos.DrawWireSphere(startPos + inputLine, 0.1f);
        }

        // ------------------------------
        // 2. 青色：実際の移動速度 (Actual Velocity)
        // ------------------------------
        Gizmos.color = Color.blue;
        
        // CharacterControllerの実際の速度を取得
        Vector3 currentVelocity = movement.Controller.velocity;
        
        // 速度ベクトルのまま描画（速いほど長くなる）
        Gizmos.DrawRay(startPos, currentVelocity);
        
        // ------------------------------
        // 3. 赤色：体の向き (Facing Direction)
        // ------------------------------
        Gizmos.color = Color.red;
        Gizmos.DrawRay(startPos, transform.forward * 1.5f);
    }
}