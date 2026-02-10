using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.VisualScripting;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance {get; private set;}

    [Header("Game Settings")]

    [Header("UI references")]
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
        score = 0;
        isPlaying = true;

        if (stateText != null) stateText.text = "";
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    void Update()
    {
        if (!isPlaying) return;

        //UI更新
        UpdateUI();
    }

    //箱から呼ばれるメソッド
    public void AddScore()
    {
        if (!isPlaying) return;

        score += 100;

        //演出
        Debug.Log("Box Destroyed!");
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

        Debug.Log("GAME OVER...");
        if (resultPanel != null) resultPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (cameraScript != null) cameraScript.enabled = false;
    }

    private void UpdateUI()
    {
        
    }

    public void RetryGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}