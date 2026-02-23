using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIHealthBar : MonoBehaviour
{
    [SerializeField] private Slider HPslider;
    [SerializeField] private bool lookAtCamera = false;
    private Camera mainCamera;

    [Header("Visibility")]
    [Tooltip("初期状態でHPバー(Slider)を表示するか(プレイヤーはON、敵はOFF推奨)")]
    [SerializeField] private bool showOnStart = true;
    [SerializeField] private float fadeDuration = 0.2f;

    [Header("Scaling")]
    [Tooltip("画面上でのサイズを一定に保つか(World Space用")]
    [SerializeField] private bool keepConstantSize = false;
    [Tooltip("サイズ計算の基準となるカメラとの距離")]
    [SerializeField] private float referenceDistance = 5f;
    private Vector3 initialScale;

    [Header("Damage Text")]
    [Tooltip("ダメージ数値を表示するTextMeshProをここにアタッチ")]
    [SerializeField] private TextMeshProUGUI damageText;
    [Tooltip("テキストが表示されている時間")]
    [SerializeField] private float damageTextDuration = 0.8f;
    [Tooltip("テキストが上に浮き上がるスピード")]
    [SerializeField] private float damageTextFloatSpeed = 50f;

    private Coroutine damageTextCoroutine;
    private Vector2 initialTextPos;

    private CanvasGroup sliderCanvasGroup;
    private Coroutine fadeCoroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = Camera.main;
        initialScale = transform.localScale;    // 最初に設定されたサイズを記憶しておく

        initialTextPos = damageText.rectTransform.anchoredPosition; // テキストの初期位置を記憶し、最初は非表示にしておく
        damageText.gameObject.SetActive(false);

        sliderCanvasGroup = HPslider.GetComponent<CanvasGroup>();
        if (sliderCanvasGroup == null)
        {
            sliderCanvasGroup = HPslider.gameObject.AddComponent<CanvasGroup>();
        }

        ShowHealthBar(showOnStart, true);
    }
    
    public void ShowHealthBar(bool show, bool instant = false)    // HPバー（Slider部分のみ）の表示・非表示を切り替えるメソッド
    {
        if (instant)
        {
            sliderCanvasGroup.alpha = show ? 1f : 0f;
            HPslider.gameObject.SetActive(show);
        }
        else
        {
            // 表示する時はまずSetActiveをtrueにしてから透明度を上げていく
            if (show) HPslider.gameObject.SetActive(true);
            fadeCoroutine = StartCoroutine(FadeRoutine(show ? 1f : 0f));
        }
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float startAlpha = sliderCanvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            sliderCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            yield return null;
        }

        sliderCanvasGroup.alpha = targetAlpha;

        // 完全に透明になったらSetActiveをfalseにして負荷を下げる
        if (targetAlpha <= 0f)
        {
            HPslider.gameObject.SetActive(false);
        }
    }

    public void Initialize(int maxHP)
    {
        HPslider.maxValue = maxHP;
        HPslider.value = maxHP;
    }

    public void UpdateHP(int currentHP)
    {
        HPslider.value = currentHP;
    }

    public void ShowDamageText(int damage)  // ダメージ数値をポップアップ表示するメソッド
    {
        if (HPslider != null)
        {
            // すでに表示中のアニメーションがあれば止める（連続ヒット対応）
            if (damageTextCoroutine != null) StopCoroutine(damageTextCoroutine);
            damageTextCoroutine = StartCoroutine(DamageTextRoutine(damage));
        }
    }

    private IEnumerator DamageTextRoutine(int damage)
    {
        damageText.text = damage.ToString();
        damageText.gameObject.SetActive(true);

        Color originalColor = damageText.color;
        originalColor.a = 1f; // 確実に不透明からスタートさせる
        damageText.color = originalColor;
        
        // 毎回同じ初期位置からスタートさせる
        damageText.rectTransform.anchoredPosition = initialTextPos;
        
        float timer = 0f;
        while (timer < damageTextDuration)
        {
            timer += Time.deltaTime;
            float normalizedTime = timer / damageTextDuration;
            
            // 上に浮かせる
            damageText.rectTransform.anchoredPosition = initialTextPos + Vector2.up * (damageTextFloatSpeed * normalizedTime);
            
            // 徐々に透明にする
            Color newColor = originalColor;
            newColor.a = Mathf.Lerp(1f, 0f, normalizedTime);
            damageText.color = newColor;

            yield return null;
        }

        damageText.gameObject.SetActive(false);
        damageText.color = originalColor; // 次のために色を戻しておく
    }

    private void LateUpdate()
    {
        if (lookAtCamera)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }

        if (keepConstantSize)
        {
            float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
            transform.localScale = initialScale * (distance / referenceDistance);
        }
    }
}
