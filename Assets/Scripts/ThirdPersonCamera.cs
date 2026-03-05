using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;  //PlayerのRoot
    [SerializeField] private Transform playerTargetPoint;   //PlayerのTargetPoint
    [SerializeField] private Transform childCamera;

    [Header("Settings")]
    [SerializeField] private float distance = 3.0f;
    [SerializeField] private float sensitivity = 2.0f;
    [SerializeField] private float yMinLimit = -20f;
    [SerializeField] private float yMaxLimit = 80f;

    [Header("Targeting")]
    [SerializeField] private TargetingSystem targetingSystem;
    [SerializeField] private float switchTargetCooldown = 0.5f; 
    private float lastSwitchTime = 0f;

    [Header("Lock-On Framing")]
    [SerializeField] private float lockOnDistance = 3.0f;   //ロックオン中のカメラ距離
    [SerializeField, Range(0.5f, 0.9f)] private float targetEnemyScreenY = 0.6f; // 敵を画面の上から何%に置くか
    [SerializeField, Range(0.1f, 0.5f)] private float targetPlayerScreenY = 0.3f;   // プレイヤーを画面の下から30%の位置に保つ
    [SerializeField] private float pitchAdjustSpeed = 40.0f;    // 角度の調整スピード
    [SerializeField] private float lockOnFollowSpeed = 8f;      // 振り向きスピード

    [Header("Camera Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.1f; // 階段のガクガクを吸収するため

    [Header("Camera Collision")]
    [SerializeField] private float collisionRadius = 0.2f; // カメラの「太さ」（壁へのめり込み余裕）
    [SerializeField] private float minCollisionDistance = 0.5f; // これ以上はプレイヤーに近づかない（頭の中に入らないようにする）
    [SerializeField] private LayerMask obstacleMask; // 壁や床のレイヤー
    
    //Internal State
    private float currentX = 0f; //水平角度
    private float currentY = 0f; //垂直角度
    private Vector2 lookInput;

    private Vector3 holderVelocity;
    private Camera mainCamera;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;       //マウスカーソルを消してロックする
        Cursor.visible = false;
        mainCamera = childCamera.GetComponent<Camera>();

        if (target == null) target = GameObject.FindGameObjectWithTag("Player").transform;

        transform.position = playerTargetPoint.position;
        // 初期角度の適用
        Vector3 initialEuler = transform.eulerAngles;
        currentX = initialEuler.y;
        currentY = initialEuler.x;
    }

    void LateUpdate()
    {
        if (target == null || childCamera == null) return;

        transform.position = Vector3.SmoothDamp(    // Holderの追従
            transform.position, 
            playerTargetPoint.position, 
            ref holderVelocity, 
            positionSmoothTime
        );

        // --- ロックオン中：ScreenSpaceフレーミング ---
        if (targetingSystem != null && targetingSystem.IsLockedOn)
        {
            if (Mathf.Abs(lookInput.x) > 0.5f && Time.time > lastSwitchTime + switchTargetCooldown)
            {
                targetingSystem.SwitchTarget(lookInput);
                lastSwitchTime = Time.time;
            }

            Vector3 enemyPos = targetingSystem.GetTargetPosition();

            // Yaw回転（Yawのみ。敵の方向を向く）
            Vector3 flatDirToEnemy = new Vector3(enemyPos.x - transform.position.x, 0, enemyPos.z - transform.position.z);
            if (flatDirToEnemy != Vector3.zero)
            {
                float targetX = Quaternion.LookRotation(flatDirToEnemy).eulerAngles.y;
                currentX = Mathf.LerpAngle(currentX, targetX, Time.deltaTime * lockOnFollowSpeed);
            }

            // Pitch回転 (Screen Space フィードバックループ)
            Vector3 enemyViewport = mainCamera.WorldToViewportPoint(enemyPos);

            if (enemyViewport.z > 0) // 敵がカメラの前方にいる場合のみ
            {
                // 敵の画面位置のズレを計算
                float viewportError = enemyViewport.y - targetEnemyScreenY;
                
                // ズレに応じてPitch（currentY）を増減させる
                // errorがプラス（敵が上すぎ）なら、カメラを上に向ける（Pitchをマイナスへ）
                currentY -= viewportError * pitchAdjustSpeed * Time.deltaTime;
            }
            else
            {
                // 敵が背後にいる場合のフェールセーフ（一時的に数学的な角度を追う）
                Vector3 dir = enemyPos - transform.position;
                float backupPitch = Quaternion.LookRotation(dir).eulerAngles.x;
                if (backupPitch > 180) backupPitch -= 360;
                currentY = Mathf.LerpAngle(currentY, backupPitch, Time.deltaTime * lockOnFollowSpeed);
            }

            currentY = Mathf.Clamp(currentY, yMinLimit, yMaxLimit);
        }
        // --- 非ロックオン（フリー）時 ---
        else
        {
            currentX += lookInput.x * sensitivity;
            currentY -= lookInput.y * sensitivity;
            currentY = Mathf.Clamp(currentY, yMinLimit, yMaxLimit);
        }

        // Holderの回転を適用
        transform.rotation = Quaternion.Euler(currentY, currentX, 0);

        // 子カメラのローカル位置・回転を適用
        float targetDistance = (targetingSystem != null && targetingSystem.IsLockedOn) ? lockOnDistance : distance;
        
        // コリジョン判定（Holderからカメラの本来の位置に向かって、球体を飛ばす）
        Vector3 direction = -transform.forward; // Holderの真後ろ方向
        float finalDistance = targetDistance;

        // 球体が障害物にぶつかったかチェック
        if (Physics.SphereCast(transform.position, collisionRadius, direction, out RaycastHit hit, targetDistance, obstacleMask))
        {
            // ぶつかった場合、壁の少し手前（hit.distance）を新しい距離にする
            // ※ただし、minCollisionDistanceよりは近づかないように制限する
            finalDistance = Mathf.Max(hit.distance, minCollisionDistance);
        }

        Vector3 idealLocalPos = new Vector3(0, 0, -finalDistance);

        float currentDist = -childCamera.localPosition.z;

        if (finalDistance < currentDist)
        {
            // 壁が迫ってきた（距離を縮める必要がある）時は、Lerpを使わず【一瞬で】移動させる
            // ※ゆっくり移動させると、移動中に壁にめり込んで裏世界が見えてしまうため
            childCamera.localPosition = idealLocalPos;
        }
        else
        {
            // 壁から離れる（距離を伸ばす）時は、Lerpで【滑らかに】元の距離に戻す
            childCamera.localPosition = Vector3.Lerp(childCamera.localPosition, idealLocalPos, Time.deltaTime * 15f);
        }
        
        childCamera.localRotation = Quaternion.Lerp(childCamera.localRotation, Quaternion.identity, Time.deltaTime * 15f);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }
}
