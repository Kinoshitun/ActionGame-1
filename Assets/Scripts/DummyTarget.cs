using UnityEngine;
using System.Collections;

public class DummyTarget : MonoBehaviour, IDamageable
{
    private Renderer meshRenderer;
    private Color originalColor;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            originalColor = meshRenderer.material.color;
        }
    }

    public void TakeDamage(int damage, Vector3 hitPoint)
    {
        Debug.Log($"サンドバッグに {damage} のダメージ！");
        StopAllCoroutines();
        StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        if (meshRenderer != null)
        {
            meshRenderer.material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            meshRenderer.material.color = originalColor;
        }
    }
}
