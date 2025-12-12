using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    // チャージ音のような「ループ再生・停止」が必要な音は、専用のスピーカーを持つ
    [SerializeField] private AudioSource chargeSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip chargeClip;
    [SerializeField] private AudioClip attackClip;

    private void Start()
    {
        // 設定忘れ防止の安全策
        if (chargeSource == null)
        {
            // なければ自分から探す、それでもなければ作る（堅牢な設計）
            chargeSource = GetComponent<AudioSource>();
            if (chargeSource == null) chargeSource = gameObject.AddComponent<AudioSource>();
        }

        // チャージ用スピーカーの初期設定
        chargeSource.playOnAwake = false;
        chargeSource.loop = true;

        if (chargeClip != null)
        {
            chargeSource.clip = chargeClip;
        }
        else if (chargeSource.clip != null)
        {
            // PlayerAudio側のClipが空でも、AudioSource側に直接入っているならそれを採用
            chargeClip = chargeSource.clip;
        }
        else
        {
            Debug.LogWarning("PlayerAudio: チャージ音が設定されていません！");
        }
    }

    // --- 外部（PlayerController）から命令されるアクション ---

    public void PlayCharge()
    {
        // 既に鳴っているなら重ねて鳴らさない（ガード節）
        if (chargeSource.isPlaying) return;

        if (chargeClip != null)
        {
            chargeSource.clip = chargeClip; // 念のためセット
            chargeSource.Play();
        }
    }

    public void StopCharge()
    {
        if (chargeSource.isPlaying)
        {
            chargeSource.Stop();
        }
    }

    public void PlayAttack()
    {
        if (AudioManager.Instance != null && attackClip != null)
        {
            AudioManager.Instance.PlaySE(attackClip);
        }
    }
}