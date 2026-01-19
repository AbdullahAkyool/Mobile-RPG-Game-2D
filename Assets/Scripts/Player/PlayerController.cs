using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private void OnEnable()
    {
        EventManager.OnGetAttackItemForPlayer += SetCurrentAttackItem;
    }

    private void OnDisable()
    {
        EventManager.OnGetAttackItemForPlayer -= SetCurrentAttackItem;
    }

    private void SetCurrentAttackItem(AttackItemController attackItem)
    {
        attackItem.transform.position = transform.position;

        EnemyController targetEnemy = GameManager.Instance.GetRandomEnemy();
        if (targetEnemy == null || targetEnemy.IsDead)
        {
            attackItem.DespawnSelf();
            return;
        }

        attackItem.MoveToTarget(targetEnemy.transform);
    }
}
