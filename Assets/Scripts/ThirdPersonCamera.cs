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

    //Internal State
    private float currentX = 0f; //水平角度
    private float currentY = 0f; //垂直角度
    private Vector2 lookInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //マウスカーソルを消してロックする（FPS/TPSの基本）
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //ターゲット未設定ならタグで探すなどの保険
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (target == null) return;

        HandleRotation();
        HandlePosition();
    }

    private void HandleRotation()
    {
        //入力値に基づいて角度を加算
        currentX += lookInput.x * sensitivity;
        currentY -= lookInput.y * sensitivity; //Y入力は引くことで上に倒すと上を向く操作になる

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
