using UnityEngine;

public class EnemyDummy : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Renderer meshRenderer;
    [SerializeField] private Color frozenColor = new Color(0.7f, 0.9f, 1f);

    private bool isDrained = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<Renderer>();
    }

    public void OnDrain()
    {
        if (isDrained) return;

        isDrained = true;

        if (meshRenderer != null)
        {
            meshRenderer.material.color = frozenColor;
        }

        Debug.Log($"{gameObject.name} からエネルギーを奪った！");
    }
}
