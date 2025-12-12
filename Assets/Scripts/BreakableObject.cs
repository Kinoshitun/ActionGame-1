using UnityEditor;
using UnityEngine;

public class BreakableObject : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject debrisPrefab;
    [SerializeField] private float explosionForce = 15f;
    [SerializeField] private float debrisLifetime = 5f;
    [SerializeField] private AudioClip breakSound;

    public void Break(Vector3 hitDirection)
    {
        if (debrisPrefab != null)
        {
              //1.破片の生成
            GameObject brokenObj = Instantiate(debrisPrefab, transform.position, transform.rotation);

            //破片のスケールを合わせる
            brokenObj.transform.localScale = transform.localScale;

            //2.破片を飛び散らせる
            //壊れたバージョンの中にあるすべての子要素（破片）のRigidbodyを取得
            Rigidbody[] rbs = brokenObj.GetComponentsInChildren<Rigidbody>();

            foreach (Rigidbody rb in rbs)
            {
                //攻撃方向 + ランダムな拡散 + 上向きの力
                Vector3 randomDir = Random.insideUnitSphere * 0.5f;
                Vector3 forceDir = (hitDirection + randomDir + Vector3.up * 0.5f).normalized;

                rb.AddForce(forceDir * explosionForce, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * explosionForce, ForceMode.Impulse);
            }

            Destroy(brokenObj, debrisLifetime);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(breakSound);
        }

        Destroy(gameObject);
    }
}
