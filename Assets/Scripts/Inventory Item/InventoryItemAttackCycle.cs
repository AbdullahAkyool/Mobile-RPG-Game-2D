using UnityEngine;
using DG.Tweening;

public class InventoryItemAttackCycle : MonoBehaviour
{
    [Header("Bounce Animation")]
    [SerializeField] private float bounceDuration = 0.3f;
    [SerializeField] private float bounceScale = 1.2f;
    [SerializeField] private Ease bounceEase = Ease.OutQuad;

    private InventoryItemController controller;
    private InventoryItemPlacement placement;
    private InventoryItemView view;

    private Tween cooldownTween;
    private Tween bounceTween;

    private void Awake()
    {
        controller = GetComponent<InventoryItemController>();
        placement = GetComponent<InventoryItemPlacement>();
        view = GetComponent<InventoryItemView>();
    }

    private void OnEnable()
    {
        EventManager.OnItemPlaced += HandleItemPlaced;
        EventManager.OnItemRemoved += HandleItemRemoved;
        EventManager.OnEnemySpawned += HandleEnemySpawned;
        EventManager.OnEnemyDied += HandleEnemyDied;
    }

    private void OnDisable()
    {
        EventManager.OnItemPlaced -= HandleItemPlaced;
        EventManager.OnItemRemoved -= HandleItemRemoved;
        EventManager.OnEnemySpawned -= HandleEnemySpawned;
        EventManager.OnEnemyDied -= HandleEnemyDied;
        KillAllTweens();
    }

    private void HandleItemPlaced(BaseGridController grid, InventoryItemController item)
    {
        if (item != controller) return;
        if (grid is not CoordinateGridController) return;

        StartCooldown();
    }

    private void HandleItemRemoved(BaseGridController grid, InventoryItemController item)
    {
        if (item != controller) return;
        if (grid is not CoordinateGridController) return;

        StopCooldown();
    }

    private void HandleEnemySpawned(EnemyController enemy)
    {
        ResumeCooldown();
    }

    private void HandleEnemyDied(EnemyController enemy)
    {
        if (GameManager.Instance != null && GameManager.Instance.ActiveEnemies.Count == 0)
        {
            PauseCooldown();
        }
    }

    private void StartCooldown()
    {
        KillAllTweens();

        if (view == null || view.OutlineImage == null)
            return;

        float duration = controller.Model.LevelData.cooldownTime;
        if (duration <= 0f) return;

        view.OutlineImage.fillAmount = 1f;

        cooldownTween = view.OutlineImage
            .DOFillAmount(0f, duration)
            .SetEase(Ease.Linear)
            .OnComplete(OnCooldownComplete);

        if (GameManager.Instance != null && GameManager.Instance.ActiveEnemies.Count == 0)
        {
            if (cooldownTween != null && cooldownTween.IsActive())
            {
                cooldownTween.Pause();
            }
        }
    }

    private void OnCooldownComplete()
    {
        PlayBounceAnimation();
    }

    private void PlayBounceAnimation()
    {
        if (view == null) return;

        KillBounceTween();

        bounceTween = transform
            .DOScale(bounceScale, bounceDuration / 2f)
            .SetEase(bounceEase)
            .OnComplete(() =>
            {
                bounceTween = transform
                    .DOScale(1f, bounceDuration / 2f)
                    .SetEase(bounceEase)
                    .OnComplete(OnBounceComplete);
            });
    }

    private void OnBounceComplete()
    {
        CreateAttackItem();

        if (placement.CurrentGrid is CoordinateGridController)
        {
            StartCooldown();
        }
    }

    private void StopCooldown()
    {
        KillAllTweens();

        if (view != null && view.OutlineImage != null)
            view.OutlineImage.fillAmount = 0f;
    }

    private void PauseCooldown()
    {
        if (cooldownTween != null && cooldownTween.IsActive() && cooldownTween.IsPlaying())
        {
            cooldownTween.Pause();
        }
    }

    private void ResumeCooldown()
    {
        if (cooldownTween != null && cooldownTween.IsActive() && !cooldownTween.IsPlaying())
        {
            cooldownTween.Play();
        }
    }

    private void KillAllTweens()
    {
        KillCooldownTween();
        KillBounceTween();
    }

    private void KillCooldownTween()
    {
        if (cooldownTween != null && cooldownTween.IsActive())
            cooldownTween.Kill();

        cooldownTween = null;
    }

    private void KillBounceTween()
    {
        if (bounceTween != null && bounceTween.IsActive())
            bounceTween.Kill();

        bounceTween = null;
        transform.localScale = Vector3.one;
    }

    private void CreateAttackItem()
    {
        ItemType itemType = controller.Model.Data.itemType;
        PoolKey attackPoolKey = PoolKey.AttackItem;

        var attackItem = PoolManager.Instance.Spawn<AttackItemController>(attackPoolKey);
        if (attackItem == null)
        {
            Debug.LogWarning($"[InventoryItemAttackCycle] Failed to spawn attack item for {itemType}");
            return;
        }

        attackItem.Initialize(controller.Model);

        EventManager.OnGetAttackItemForPlayer?.Invoke(attackItem);
    }
}
