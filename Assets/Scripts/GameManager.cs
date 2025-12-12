using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.VisualScripting;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance {get; private set;}

    [Header("Game Settings")]
    [SerializeField] private float initialTime = 30f;       //制限時間
    [SerializeField] private float timeBonusPerBox = 2f;    //箱破壊ボーナスタイム

    [Header("UI references")]
    [SerializeField] private TextMeshProUGUI timeText;      //残り時間表示
    [SerializeField] private TextMeshProUGUI stateText;     //"GOAL!" "GAME OVER"表示
    [SerializeField] private GameObject resultPanel;        //リザルト画面

    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private MonoBehaviour cameraScript;

    [Header("Audio")]
    [SerializeField] private AudioClip goalSound;

    //内部変数
    private float currentTime;
    private int score;
    private bool isPlaying = false;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        //初期化
        currentTime = initialTime;
        score = 0;
        isPlaying = true;

        if (stateText != null) stateText.text = "";
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    void Update()
    {
        if (!isPlaying) return;

        //タイマー減算
        currentTime -= Time.deltaTime;

        //ゲームオーバー判定
        if (currentTime <= 0)
        {
            currentTime = 0;
            GameOver();
        }

        //UI更新
        UpdateUI();
    }

    //箱から呼ばれるメソッド
    public void AddScore()
    {
        if (!isPlaying) return;

        score += 100;
        currentTime += timeBonusPerBox;

        //演出
        Debug.Log("Box Destroyed! Time Extended!");
    }

    //ゴールエリアから呼ばれるメソッド
    public void Goal()
    {
        if (!isPlaying) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySE(goalSound);
        }

        isPlaying = false;
        if (player != null) player.isInputEnabled = false;

        Debug.Log("GOAL!");
        if (stateText != null) stateText.text = "CLEAR!!";
        if (resultPanel != null) resultPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (cameraScript != null) cameraScript.enabled = false;
    }

    private void GameOver()
    {
        isPlaying = false;

        if (player != null) player.isInputEnabled = false;

        Debug.Log("GAME OVER...");
        if (stateText != null) stateText.text = "TIME UP...";
        if (resultPanel != null) resultPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (cameraScript != null) cameraScript.enabled = false;
    }

    private void UpdateUI()
    {
        if (timeText != null)
        {
            timeText.text = $"TIME: {currentTime:F2}\nSCORE: {score}";

            //残り時間が少ないと赤くする
            timeText.color = (currentTime < 5f) ? Color.red : Color.white;
        }
    }

    public void RetryGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}