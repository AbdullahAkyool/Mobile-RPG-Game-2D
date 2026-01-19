using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ParticleEffectDatabaseSO", menuName = "ScriptableObjects/Particle System/Particle Effect Database")]
public class ParticleEffectDatabaseSO : ScriptableObject
{
    public List<ParticleEffectData> ParticleEffects = new List<ParticleEffectData>();

    public ParticleEffectData GetParticleEffect(ParticleEffectKey key)
    {
        return ParticleEffects.Find(pe => pe.ParticleEffectKey == key);
    }
}

[Serializable]
public class ParticleEffectData
{
    public ParticleEffectKey ParticleEffectKey;
    public ParticleEffectController EffectObject;
}

public enum ParticleEffectKey
{
    None = 0,
    Hit = 1,
    Die = 2,
    Spawn = 3,
}
