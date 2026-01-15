using UnityEngine;

public class EnergyOrb : MonoBehaviour
{
    private Transform target;
    private PlayerController playerController;
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private float arrivalDistance = 0.5f;
    [SerializeField] private float energyAmount = 10f;

    public void Initialize(Transform targetPlayer)
    {
        target = targetPlayer;
        playerController = targetPlayer.GetComponent<PlayerController>();
        // 2秒経っても届かなければ消す（安全策）
        Destroy(gameObject, 2.0f);
    }

    void Update()
    {
        if (target == null) return;

        // ターゲットに向かって移動
        transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

        // 到着判定
        if (Vector3.Distance(transform.position, target.position) < arrivalDistance)
        {
            if (playerController != null) playerController.AddEnergy(energyAmount);
            // ここでプレイヤーにエネルギー加算処理を呼ぶことも可能
            Destroy(gameObject);
        }
    }
}
