using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0, 1.5f, 0); //足元ではなく背中当たりを見るようにずらす

    [Header("Settings")]
    [SerializeField] private float distance = 5.0f;
    [SerializeField] private float sensitivity = 2.0f;
    [SerializeField] private float yMinLimit = -20f;
    [SerializeField] private float yMaxLimit = 80f;

    [Header("Targeting")]
    [SerializeField] private TargetingSystem targetingSystem;
    [SerializeField] private float lockOnFollowSpeed = 10f;

    // ★追加：見下ろし角度の最小値（これ以上カメラが下に行かなくなる）
    [SerializeField] private float minLockOnAngleY = 15f; 
    
    // ★追加：ターゲット切り替えのクールダウン時間（連続で切り替わるのを防ぐ）
    [SerializeField] private float switchTargetCooldown = 0.5f; 
    private float lastSwitchTime = 0f;

    //Internal State
    private float currentX = 0f; //水平角度
    private float currentY = 0f; //垂直角度
    private Vector2 lookInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;       //マウスカーソルを消してロックする
        Cursor.visible = false;

        if (target == null)     //ターゲット未設定ならタグで探すなどの保険
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleRotation();
        HandlePosition();
    }

    private void HandleRotation()
    {
        if (targetingSystem != null && targetingSystem.IsLockedOn)
        {
            // 右スティックが大きく倒されたら、ターゲット切り替えを実行する
            if (Mathf.Abs(lookInput.x) > 0.5f && Time.time > lastSwitchTime + switchTargetCooldown)
            {
                targetingSystem.SwitchTarget(lookInput);
                lastSwitchTime = Time.time;
            }

            // --- アングルの計算 ---
            Vector3 targetCenter = targetingSystem.GetTargetPosition(); 
            Vector3 myCenter = target.position + targetOffset;

            Vector3 dirToTarget = (targetCenter - myCenter).normalized;
            
            float targetX = Mathf.Atan2(dirToTarget.x, dirToTarget.z) * Mathf.Rad2Deg;
            float targetY = -Mathf.Asin(dirToTarget.y) * Mathf.Rad2Deg + 10f;
            targetY = Mathf.Max(targetY, minLockOnAngleY);

            currentX = Mathf.LerpAngle(currentX, targetX, Time.deltaTime * lockOnFollowSpeed);
            currentY = Mathf.LerpAngle(currentY, targetY, Time.deltaTime * lockOnFollowSpeed);
        }
        else
        {
            //入力値に基づいて角度を加算
            currentX += lookInput.x * sensitivity;
            currentY -= lookInput.y * sensitivity; //Y入力は引くことで上に倒すと上を向く操作になる
        }
        
        //上下の角度制限
        currentY = Mathf.Clamp(currentY, yMinLimit, yMaxLimit);
    }

    private void HandlePosition()
    {
        //回転の計算
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        //プレイヤーの位置＋オフセット
        Vector3 targetPos = target.position + targetOffset;

        //カメラの位置計算:
        //ターゲット位置から、回転方向の逆（後ろ）にdistance分だけ下がったと場所
        Vector3 position = targetPos - (rotation * Vector3.forward * distance);

        //反映
        transform.rotation = rotation;
        transform.position = position;
    }

    //InputSystemからの入力を受けるメソッド
    //PlayerInputコンポーネントのEvents -> "OnLook"に割り当てるか、
    //SendMessageを使う場合はこのメソッド名が自動で呼ばれる。
    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }
}
