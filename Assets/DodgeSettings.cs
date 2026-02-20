using UnityEngine;

[CreateAssetMenu(fileName = "NewDodgeSettings", menuName = "Game/Dodge Settings")]
public class DodgeSettings : ScriptableObject
{
    [Header("Basic")]
    public float distance = 5.0f;     // 移動距離 (メートル)
    public float duration = 0.8f;     // 全体の動作時間 (秒)

    [Header("Timing (0.0 to 1.0)")]
    [Range(0f, 1f)] public float moveStartRatio = 0.2f; // 20%の時点から動き出し (発生)
    [Range(0f, 1f)] public float moveEndRatio = 0.8f;   // 80%の時点で停止 (硬直開始)
    
    // 無敵時間は移動時間と同じにするなら変数は不要ですが、
    // 別にしたい場合はここに追加してください。今回は「移動中＝無敵」とします。
}