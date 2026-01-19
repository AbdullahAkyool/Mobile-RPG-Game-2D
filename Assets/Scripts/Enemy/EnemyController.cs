using UnityEngine;

public class EnemyController : MonoBehaviour, IPoolable
{
    [Header("Identity")]
    [SerializeField] private PoolKey poolKey;
    public PoolKey PoolKey => poolKey;

    [Header("Health")]
    [SerializeField] private EnemyHealthController healthController;

    public bool IsDead => healthController == null || healthController.CurrentHealth <= 0;

    public void OnSpawn()
    {
        if (healthController != null)
        {
            healthController.ResetHealth();
        }

        gameObject.SetActive(true);
    }

    public void OnDespawn()
    {
        gameObject.SetActive(false);
    }

    public void TakeDamage(int damage)
    {
        if (healthController == null) return;
        
        healthController.TakeDamage(damage);

        ParticleEffectController hitParticle = PoolManager.Instance.Spawn<ParticleEffectController>(PoolKey.ParticleEffect_Hit);
        if (hitParticle != null)
        {
            hitParticle.transform.position = transform.position;
        }
    }
}
