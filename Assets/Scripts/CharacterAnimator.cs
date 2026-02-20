using UnityEngine;

[DefaultExecutionOrder(100)] 
public class CharacterAnimator : MonoBehaviour
{    
    public Animator Animator { get; private set; }

    private static readonly int AnimID_HorizontalSpeed = Animator.StringToHash("HorizontalSpeed");
    private static readonly int AnimID_VerticalSpeed = Animator.StringToHash("VerticalSpeed");
    private static readonly int AnimID_ActionSpeed = Animator.StringToHash("ActionSpeed");
    private static readonly int AnimID_IsGuarding = Animator.StringToHash("IsGuarding"); // ★追加
    // private static readonly int AnimID_IsGrounded = Animator.StringToHash("IsGrounded");

    private static readonly int LocomotionState = Animator.StringToHash("Locomotion");
    private static readonly int AirborneState = Animator.StringToHash("Airborne");
    private static readonly int BackstepState = Animator.StringToHash("Backstep");
    private static readonly int RollState = Animator.StringToHash("Roll");
    private static readonly int DiveState = Animator.StringToHash("Dive");
    private static readonly int HardLandingState = Animator.StringToHash("HardLanding");

    private static readonly int AnimID_Attack1 = Animator.StringToHash("Attack1");
    private static readonly int AnimID_Attack2 = Animator.StringToHash("Attack2");
    private static readonly int AnimID_Attack3 = Animator.StringToHash("Attack3");
    // private readonly int AnimID_Damage = Animator.StringToHash("Damage");

    public int Locomotion => LocomotionState;
    public int Airborne => AirborneState;
    public int Backstep => BackstepState;
    public int Roll => RollState;
    public int Dive => DiveState;
    public int HardLanding => HardLandingState;
    public int Attack1 => AnimID_Attack1;
    public int Attack2 => AnimID_Attack2;
    public int Attack3 => AnimID_Attack3;

    [Header("Guard Stance Offsets (手動調整用)")]
    [Tooltip("★テスト用★ チェックを入れると歩き判定などを無視して常にオフセットを適用します！")]
    public bool forceApplyOffset = false;
    
    [Tooltip("★テスト用★ チェックを入れると、回転の掛け算の順序を逆にします（相殺される場合用）")]
    public bool useReverseMultiplication = false;

    [Header("Guard Stance Offsets (手動調整用)")]
    [Tooltip("ガード中の腰のひねり補正")]
    public Vector3 guardSpineOffset = new Vector3(0f, 45f, 0f);
    [Tooltip("ガード中の左肩の角度補正")]
    public Vector3 guardLeftShoulderOffset = new Vector3(20f, 0f, -10f);
    [Tooltip("ガード中の左腕（盾）の角度補正")]
    public Vector3 guardLeftArmOffset = new Vector3(0f, -20f, 0f);

    [Header("Bones (インスペクターで手動アタッチ用)")]
    [SerializeField] private Transform spineBone;
    [SerializeField] private Transform leftShoulderBone;
    [SerializeField] private Transform leftArmBone;
    [SerializeField] private bool isGuardingCurrent;
    [SerializeField] private bool isGuardMovingCurrent;

    private float moveGuardBlend = 0f;

    void Awake()
    {
        Animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        if (Animator != null)
        {
            spineBone = Animator.GetBoneTransform(HumanBodyBones.Spine);
            leftShoulderBone = Animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            leftArmBone = Animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        }
    }

    void LateUpdate()
    {
        if (Animator == null) return;

        // ガード中かつ移動中ならブレンド値を1へ（強制スイッチONなら無条件で1）
        float targetBlend = (isGuardingCurrent && isGuardMovingCurrent) ? 1f : 0f;
        moveGuardBlend = Mathf.Lerp(moveGuardBlend, targetBlend, Time.deltaTime * 25f);

        // ブレンド値が0より大きい時だけ骨を曲げる（Slerpを使って滑らかに角度を足す）
        if (moveGuardBlend > 0.001f)
        {
            if (spineBone != null) 
            {
                Quaternion offset = Quaternion.Euler(guardSpineOffset);
                Quaternion blendOffset = Quaternion.Slerp(Quaternion.identity, offset, moveGuardBlend);
                // ★掛け算の順序を切り替えられるようにする
                spineBone.localRotation = useReverseMultiplication ? (blendOffset * spineBone.localRotation) : (spineBone.localRotation * blendOffset);
            }
            
            if (leftShoulderBone != null) 
            {
                Quaternion offset = Quaternion.Euler(guardLeftShoulderOffset);
                Quaternion blendOffset = Quaternion.Slerp(Quaternion.identity, offset, moveGuardBlend);
                leftShoulderBone.localRotation = useReverseMultiplication ? (blendOffset * leftShoulderBone.localRotation) : (leftShoulderBone.localRotation * blendOffset);
            }
                
            if (leftArmBone != null) 
            {
                Quaternion offset = Quaternion.Euler(guardLeftArmOffset);
                Quaternion blendOffset = Quaternion.Slerp(Quaternion.identity, offset, moveGuardBlend);
                leftArmBone.localRotation = useReverseMultiplication ? (blendOffset * leftArmBone.localRotation) : (leftArmBone.localRotation * blendOffset);
            }
        }
    }

    public AnimatorStateInfo GetCurrentAnimatorStateInfo(int layerIndex)
    {
        return Animator.GetCurrentAnimatorStateInfo(layerIndex);
    }

    public void UpdateMovement(float hSpeed, float vSpeed, bool isGrounded)
    {
        if (Animator == null) return;
        Animator.SetFloat(AnimID_HorizontalSpeed, hSpeed, 0.1f, Time.deltaTime);   //Locomotion用のパラメータ更新
        Animator.SetFloat(AnimID_VerticalSpeed, vSpeed, 0.1f, Time.deltaTime);       //Airborne用のパラメータ更新
        // Animator.SetBool(AnimID_IsGrounded, isGrounded);
    }

    //外部からの命令用
    public void PlayState(int stateID, float transitionDuration = 0.1f)
    {
        Animator.CrossFadeInFixedTime(stateID, transitionDuration);
    }

    public void SetActionSpeed(float speedMultiplier)
    {
        Animator.SetFloat(AnimID_ActionSpeed, speedMultiplier);
    }

    public void SetGuarding(bool isGuarding)
    {
        isGuardingCurrent = isGuarding;
        if (Animator != null)
        {
            Animator.SetBool(AnimID_IsGuarding, isGuarding);
        }
    }

    public void SetGuardMoving(bool isMoving)
    {
        isGuardMovingCurrent = isMoving;
    }

    // public void PlayHardLanding()
    // {
    //     Animator.SetTrigger("Hard Landing");
    // }

    // public void PlaySoftLanding()
    // {
    //     Animator.SetTrigger("Landing");
    // }

    // public void PlayAttack()
    // {
    //     Animator.CrossFadeInFixedTime("Attack", 0.1f);
    // }

    // public void PlayDamage()
    // {
    //     Animator.CrossFadeInFixedTime("Damage", 0.1f);
    // }

    // public void PlayDodge(string stateName, float speedMultiplier)
    // {
    //     Animator.SetFloat(AnimID_ActionSpeed, speedMultiplier);
    //     Animator.CrossFadeInFixedTime(stateName, 0.1f);
    // }

    // public void ResetTriggers()
    // {
    //     Animator.ResetTrigger(AnimID_Jump);
    //     Animator.ResetTrigger(AnimID_Attack);
    // }
}
