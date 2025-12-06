using UnityEngine;
using System.Collections;

public class CameraShaker : MonoBehaviour
{
    // シングルトン化（どこからでも呼べるようにする便利機能）
    public static CameraShaker Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    // 揺らす関数
    public void Shake(float duration, float magnitude)
    {
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // ランダムに位置をずらす
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.unscaledDeltaTime; // ヒットストップ中も揺らすために unscaledDeltaTime を使う
            yield return null;
        }

        transform.localPosition = originalPos; // 元の位置に戻す
    }
}