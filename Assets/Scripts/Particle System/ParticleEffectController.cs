using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleEffectController : MonoBehaviour, IPoolable
{
    [Header("Identity")]
    [SerializeField] private PoolKey effectPoolKey;
    public PoolKey EffectPoolKey => effectPoolKey;

    [SerializeField] private List<ParticleSystem> particleSystems;

    private Coroutine returnToPoolCoroutine;

    public void OnSpawn()
    {
        gameObject.SetActive(true);
        PlayEffect();
        StartReturnToPoolTimer();
    }

    public void OnDespawn()
    {
        if (returnToPoolCoroutine != null)
        {
            StopCoroutine(returnToPoolCoroutine);
            returnToPoolCoroutine = null;
        }
        StopEffect();
        gameObject.SetActive(false);
    }

    private void PlayEffect()
    {
        if (particleSystems == null || particleSystems.Count == 0)
            return;
        foreach (var ps in particleSystems)
        {
            ps.Play();
        }
    }

    private void StopEffect()
    {
        if (particleSystems == null || particleSystems.Count == 0)
            return;
        foreach (var ps in particleSystems)
        {
            ps.Stop();
        }
    }

    private void StartReturnToPoolTimer()
    {
        if (returnToPoolCoroutine != null)
        {
            StopCoroutine(returnToPoolCoroutine);
        }
        returnToPoolCoroutine = StartCoroutine(ReturnToPoolAfterDuration());
    }

    private IEnumerator ReturnToPoolAfterDuration()
    {
        // En uzun particle duration'ı bul
        float maxDuration = 0f;
        if (particleSystems != null && particleSystems.Count > 0)
        {
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                {
                    float duration = ps.main.duration + ps.main.startLifetime.constantMax;
                    if (duration > maxDuration)
                        maxDuration = duration;
                }
            }
        }

        // En az 0.5 saniye bekle
        if (maxDuration < 0.5f)
            maxDuration = 0.5f;

        yield return new WaitForSeconds(maxDuration);

        // Pool'a geri dön
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Despawn(effectPoolKey, this);
        }

        returnToPoolCoroutine = null;
    }
}
