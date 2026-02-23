using UnityEngine;
using UnityEngine.InputSystem;

public class TargetingSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxTargetRange = 15f;
    [SerializeField] private LayerMask targetLayer;

    [Header("UI Marker")]
    [SerializeField] private RectTransform lockOnMarkerUI;

    public Transform CurrentTarget { get; private set; }
    public bool IsLockedOn => CurrentTarget != null;

    private Camera mainCamera;
    private Collider currentTargetCollider;
    private IDamageable currentTargetDamageable;

    private EnemyHealth currentEnemyHealth;     // 現在ロックオンしている敵のHealthスクリプトを記憶しておく

    void Start()
    {
        mainCamera = Camera.main;
        if (lockOnMarkerUI != null) lockOnMarkerUI.gameObject.SetActive(false);
    }

    void Update()
    {
        if (IsLockedOn)
        {
            bool isDead = currentTargetCollider != null && !currentTargetCollider.enabled;

            // 敵が遠ざかりすぎたり、消えたりしたらロックオン解除
            if (Vector3.Distance(transform.position, CurrentTarget.position) > maxTargetRange || 
                !CurrentTarget.gameObject.activeInHierarchy ||
                isDead)
            {
                ClearTarget();
            }
            else if (lockOnMarkerUI != null && mainCamera != null)  // マーカーを敵の位置に追従させる処理
            {
                Vector3 targetCenter = GetTargetPosition();  // 敵の中心座標＋高さ
                Vector3 screenPos = mainCamera.WorldToScreenPoint(targetCenter); // 画面の2D座標に変換
                
                if (screenPos.z > 0) // カメラの前にいる時だけ表示
                {
                    lockOnMarkerUI.gameObject.SetActive(true);
                    lockOnMarkerUI.position = screenPos; // UIを敵の位置にピタッと移動
                }
                else
                {
                    lockOnMarkerUI.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            // ロックオンが外れたらマーカーを隠す
            if (lockOnMarkerUI != null && lockOnMarkerUI.gameObject.activeSelf)
            {
                lockOnMarkerUI.gameObject.SetActive(false);
            }
        }
    }

    public void OnLockOn(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (IsLockedOn) ClearTarget(); // 既にロックオン中なら解除
            else FindTarget();  // ロックオン対象を探す
        }
    }

    private void FindTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, maxTargetRange, targetLayer);
        Transform bestTarget = null;
        Collider bestCollider = null;
        IDamageable bestDamageable = null; 

        float closestDistance = Mathf.Infinity;
        Vector2 screenCenter = new Vector2(0.5f, 0.5f);

        foreach (Collider col in colliders)
        {
            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            if (damageable != null && mainCamera != null)
            {
                Vector3 viewportPos = mainCamera.WorldToViewportPoint(col.bounds.center);
                
                if (viewportPos.z > 0)
                {
                    // 画面中央からの距離を計算
                    float distance = Vector2.Distance(screenCenter, new Vector2(viewportPos.x, viewportPos.y));
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        bestTarget = col.transform;
                        bestCollider = col;
                        bestDamageable = damageable;
                    }
                }
            }
        }

        if (bestTarget != null)
        {
            CurrentTarget = bestTarget;
            currentTargetCollider = bestCollider;
            currentTargetDamageable = bestDamageable;

            currentEnemyHealth = currentTargetDamageable as EnemyHealth;
            if (currentEnemyHealth != null) currentEnemyHealth.SetLockOnState(true);
        }
    }

    public void SwitchTarget(Vector2 inputDir)
    {
        if (!IsLockedOn || mainCamera == null) return;

        float switchDir = Mathf.Sign(inputDir.x);   // 入力方向(1: 右, -1: 左)

        Collider[] colliders = Physics.OverlapSphere(transform.position, maxTargetRange, targetLayer);
        Transform bestTarget = null;
        Collider bestCollider = null;
        IDamageable bestDamageable = null;
        float closestScreenDistance = Mathf.Infinity;

        Vector3 currentViewportPos = mainCamera.WorldToViewportPoint(GetTargetPosition());

        foreach (Collider col in colliders)
        {
            if (col.transform == CurrentTarget) continue; // 今のターゲットは除外

            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                // 現在のターゲットから見て、新しい敵が画面の左右どちらにいるかを計算
                Vector3 targetViewportPos = mainCamera.WorldToViewportPoint(col.bounds.center);

                if (targetViewportPos.z <= 0) continue;
                
                // カメラの右方向ベクトルとの内積をとり、画面右側にいるか左側にいるかを判定
                float xDiff = targetViewportPos.x - currentViewportPos.x;
                float dirSign = Mathf.Sign(xDiff);
                
                // 入力した方向（左右）にいる敵だけを候補にする
                if (dirSign == switchDir)
                {
                    // 画面上での距離を計算（現在の敵に一番近いものを次のターゲットとする）
                    Vector2 screenDiff = new Vector2(targetViewportPos.x, targetViewportPos.y) - new Vector2(currentViewportPos.x, currentViewportPos.y);
                    float distance = screenDiff.magnitude;

                    if (distance < closestScreenDistance)
                    {
                        closestScreenDistance = distance;
                        bestTarget = col.transform;
                        bestCollider = col;
                        bestDamageable = damageable;
                    }
                }
            }
        }

        if (bestTarget != null)
        {
            if (currentEnemyHealth != null) currentEnemyHealth.SetLockOnState(false);   // いままでロックオンしていた敵のHPバーを消す

            CurrentTarget = bestTarget;
            currentTargetCollider = bestCollider;
            currentTargetDamageable = bestDamageable;;

            currentEnemyHealth = currentTargetDamageable as EnemyHealth;
            if (currentEnemyHealth != null) currentEnemyHealth.SetLockOnState(true);
        }
    }

    public void ClearTarget()
    {
        if (currentEnemyHealth != null)     // ロックオン解除時にHPバーを消す
        {
            currentEnemyHealth.SetLockOnState(false);
            currentEnemyHealth = null;
        }

        CurrentTarget = null;
        currentTargetCollider = null;
        currentTargetDamageable = null; 
    }

    public Vector3 GetTargetPosition()      // ターゲットの正確な中心座標を返すメソッド（カメラ等からも呼べるようにする）
    {
        if (currentTargetDamageable != null && currentTargetDamageable.TargetPoint != null)
        {
            return currentTargetDamageable.TargetPoint.position;
        }
        else if (currentTargetCollider != null)
        {
            return currentTargetCollider.bounds.center;
        }
        else if (CurrentTarget != null)
        {
            return CurrentTarget.position + Vector3.up * 1.0f; // フォールバック
        }
        return transform.position;
    }
}
