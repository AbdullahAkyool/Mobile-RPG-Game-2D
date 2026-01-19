using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class AttackItemController : MonoBehaviour, IPoolable
{
    [Header("Identity")]
    [SerializeField] private PoolKey itemPoolKey;
    public PoolKey ItemPoolKey => itemPoolKey;

    [SerializeField] private SpriteRenderer itemImage;
    private int damagePower;

    [Header("Movement")]
    [SerializeField] private float moveDuration = 0.4f;
    [SerializeField] private float jumpPower = 2f;
    [SerializeField] private int numJumps = 1;
    [SerializeField] private Ease moveEase = Ease.OutQuad;

    [Header("Rotation")]
    [SerializeField] private bool rotateWhileMoving = true;
    [SerializeField] private float rotationSpeed = 360f;

    private Tween moveTween;
    private Tween rotateTween;
    private InventoryItemModel sourceItemModel;

    public void OnSpawn()
    {
        gameObject.SetActive(true);
    }

    public void OnDespawn()
    {
        KillAllTweens();
        gameObject.SetActive(false);
    }

    public void Initialize(InventoryItemModel sourceModel)
    {
        sourceItemModel = sourceModel;

        if (itemImage != null && sourceModel.LevelData.itemIcon != null)
        {
            itemImage.sprite = sourceModel.LevelData.itemIcon;
        }

        damagePower = sourceModel.LevelData.damagePower;
    }

    public void MoveToTarget(Transform target)
    {
        if (target == null)
        {
            DespawnSelf();
            return;
        }

        KillAllTweens();

        // DOJump kullanarak bombeli hareket
        moveTween = transform.DOJump(target.position, jumpPower, numJumps, moveDuration)
            .SetEase(moveEase)
            .OnComplete(() => OnReachedTarget(target));

        // Z ekseninde sürekli dönme
        if (rotateWhileMoving)
        {
            rotateTween = transform.DORotate(new Vector3(0, 0, -360f), rotationSpeed / 360f, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Restart)
                .SetEase(Ease.Linear);
        }
    }

    private void OnReachedTarget(Transform target)
    {
        if (target.TryGetComponent<EnemyController>(out var enemy))
        {
            enemy.TakeDamage(damagePower);
        }

        DespawnSelf();
    }


    private void KillAllTweens()
    {
        if (moveTween != null && moveTween.IsActive())
            moveTween.Kill();
        moveTween = null;

        if (rotateTween != null && rotateTween.IsActive())
            rotateTween.Kill();
        rotateTween = null;

        // Reset rotation
        transform.rotation = Quaternion.identity;
    }

    public void DespawnSelf()
    {
        PoolManager.Instance.Despawn(itemPoolKey, this);
    }
}
