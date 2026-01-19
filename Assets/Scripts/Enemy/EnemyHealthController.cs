using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthController : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;
    public int CurrentHealth => currentHealth;

    [SerializeField] private Image healthBarImage;
    [SerializeField] private TMP_Text healthText;

    private void Awake()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        ParticleEffectController dieParticle = PoolManager.Instance.Spawn<ParticleEffectController>(PoolKey.ParticleEffect_Die);
        if (dieParticle != null)
        {
            dieParticle.transform.position = transform.position;
        }

        EnemyController enemy = GetComponentInParent<EnemyController>();
        if (enemy != null && GameManager.Instance != null)
        {
            GameManager.Instance.OnEnemyDied(enemy);
        }
    }

    public void UpdateHealthBar()
    {
        healthBarImage.fillAmount = (float)currentHealth / maxHealth;
        healthText.text = $"{currentHealth}";
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
    }
}
