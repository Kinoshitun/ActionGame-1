using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;

public class TargetingSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxTargetRange = 15f;
    [SerializeField] private LayerMask targetLayer;

    [Header("UI Marker")]
    [SerializeField] private RectTransform lockOnMarkerUI;
    [SerializeField] private Vector3 markerOffset = new Vector3(0, 1.0f, 0);

    public Transform CurrentTarget { get; private set; }
    public bool IsLockedOn => CurrentTarget != null;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (lockOnMarkerUI != null) lockOnMarkerUI.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (IsLockedOn)
        {
            // 敵が遠ざかりすぎたり、消えたりしたらロックオン解除
            if (Vector3.Distance(transform.position, CurrentTarget.position) > maxTargetRange || !CurrentTarget.gameObject.activeInHierarchy)
            {
                ClearTarget();
            }
            // マーカーを敵の位置に追従させる処理
            else if (lockOnMarkerUI != null && mainCamera != null)
            {
                Vector3 targetPos = CurrentTarget.position + markerOffset; // 敵の中心座標＋高さ
                Vector3 screenPos = mainCamera.WorldToScreenPoint(targetPos); // 画面の2D座標に変換
                
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
        float closestDistance = Mathf.Infinity;
        Vector2 screenCenter = new Vector2(0.5f, 0.5f);

        foreach (Collider col in colliders)
        {
            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            if (damageable != null && mainCamera != null)
            {
                Vector3 viewportPos = mainCamera.WorldToViewportPoint(col.transform.position);
                
                if (viewportPos.z > 0)
                {
                    // 画面中央からの距離を計算
                    float distance = Vector2.Distance(screenCenter, new Vector2(viewportPos.x, viewportPos.y));
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        bestTarget = col.transform;
                    }
                }
            }
        }

        if (bestTarget != null)
        {
            CurrentTarget = bestTarget;
        }
    }

    public void SwitchTarget(Vector2 inputDir)
    {
        if (!IsLockedOn || mainCamera == null) return;

        float switchDir = Mathf.Sign(inputDir.x);   // 入力方向（1: 右, -1: 左）

        Collider[] colliders = Physics.OverlapSphere(transform.position, maxTargetRange, targetLayer);
        Transform bestTarget = null;
        float closestScreenDistance = Mathf.Infinity;

        Vector3 currentViewportPos = mainCamera.WorldToViewportPoint(CurrentTarget.position);

        foreach (Collider col in colliders)
        {
            if (col.transform == CurrentTarget) continue; // 今のターゲットは除外

            IDamageable damageable = col.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                // 現在のターゲットから見て、新しい敵が画面の左右どちらにいるかを計算
                Vector3 targetViewportPos = mainCamera.WorldToViewportPoint(col.transform.position);

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
                    }
                }
            }
        }

        if (bestTarget != null)
        {
            CurrentTarget = bestTarget;
        }
    }

    public void ClearTarget()
    {
        CurrentTarget = null;
    }
}

