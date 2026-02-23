using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHP = 100;   //最大HP
    private int currentHP;                      //現在HP

    [Header("Targeting")]
    [Tooltip("敵がプレイヤーをロックオンするときの基準点")]
    [SerializeField] private Transform lockOnPoint;
    public Transform TargetPoint => lockOnPoint;

    [Header("UI")]
    [SerializeField] private UIHealthBar hpBar;

    private PlayerController playerController;
    private CharacterCombat characterCombat;

    void Awake()
    {
        currentHP = maxHP;
        playerController = GetComponent<PlayerController>();
        characterCombat = GetComponent<CharacterCombat>();
    }

    private void Start()
    {
        hpBar.Initialize(maxHP);
    }

    public void TakeDamage(int damage, Vector3 hitPosition)
    {
        if (currentHP <= 0) return;
        if (characterCombat.IsInvincible) return;   //無敵状態なら終了

        if (playerController.IsGuarding)    //ガード状態の場合の処理
        {
            Vector3 dirToHit = (hitPosition - transform.position).normalized;   //被弾地点からプレイヤーの地点への方向ベクトル
            dirToHit.y = 0; //xz成分のみ残す
            float angle = Vector3.Angle(transform.forward, dirToHit);   //正面方向と被弾方向とのなす角を計算

            if (angle <= 70f)       //前方140°以内からの攻撃ならガード成功
            {
                Debug.Log("ガード成功");
                playerController.OnGuardSuccess();
                return;             //ダメージ無効
            }
            else    //ガード範囲外の場合被弾
            {
                Debug.Log("めくり攻撃被弾！");
            }
        }

        //ガード失敗した場合の処理
        currentHP -= damage;
        Debug.Log("$プレイヤーに {damage} のダメージ！ 残りHP: {currentHP}");
        hpBar.UpdateHP(currentHP);

        if (currentHP <= 0) //HPが0以下のとき、死亡
        {
            Debug.Log("YOU DIED");
            playerController.OnDie();
        }
        else    //HPが残ってる場合、被弾
        {
            playerController.OnTakeDamage();
        }
        
    }
}
