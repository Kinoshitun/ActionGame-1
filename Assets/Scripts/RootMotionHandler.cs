using UnityEngine;

public class RootMotionHandler : MonoBehaviour
{
    // 親のMovementスクリプトへの参照
    [SerializeField] private CharacterMovement characterMovement;
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
        
        // 親のMovementがセットされていなければ、自動で親から探す
        if (characterMovement == null)
        {
            characterMovement = GetComponentInParent<CharacterMovement>();
        }
    }

    // このメソッドは、Animatorと同じオブジェクトにあるので正しく呼ばれる！
    void OnAnimatorMove()
    {
        // 親のMovementが存在し、Root Motionを使う設定になっている時だけ伝える
        if (characterMovement != null/* && characterMovement.UseRootMotion*/)
        {
            Debug.Log($"DeltaPos: {animator.deltaPosition.magnitude}");
            // アニメーターが計算した「このフレームの移動量」を親に渡す
            characterMovement.ApplyRootMotion(animator.deltaPosition);
        }
    }
}