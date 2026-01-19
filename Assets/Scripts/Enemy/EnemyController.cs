using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private EnemyHealthController healthController;

    public bool IsDead => healthController == null || healthController.CurrentHealth <= 0;

    public void TakeDamage(int damage)
    {
        if (healthController == null) return;
        
        healthController.TakeDamage(damage);

        ParticleEffectController particleEffect = PoolManager.Instance.Spawn<ParticleEffectController>(PoolKey.ParticleEffect_Hit);
        
        if (particleEffect != null)
        {
            particleEffect.transform.position = transform.position;
        }
    }
}
