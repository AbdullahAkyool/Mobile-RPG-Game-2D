using System.Collections.Generic;
using UnityEngine;

public class ParticleEffectController : MonoBehaviour, IPoolable
{
    [Header("Identity")]
    [SerializeField] private PoolKey effectPoolKey;
    public PoolKey EffectPoolKey => effectPoolKey;

    [SerializeField] private List<ParticleSystem> particleSystems;

    public void OnSpawn()
    {
        gameObject.SetActive(true);
        PlayEffect();
    }

    public void OnDespawn()
    {
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
}
