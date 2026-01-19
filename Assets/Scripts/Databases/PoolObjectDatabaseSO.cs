using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PoolObjectDatabaseSO", menuName = "ScriptableObjects/Pool System/Pool Object Database")]
public class PoolObjectDatabaseSO : ScriptableObject
{
    [SerializeField] private List<PoolObjectData> PoolObjects;

    public PoolObjectData GetPoolObjectData(PoolKey type)
    {
        if (PoolObjects == null) return null;
        return PoolObjects.Find(po => po.Type == type);
    }

    public List<PoolObjectData> GetPoolObjectDataList()
    {
        // Null ise boş liste döndür (NullReferenceException önleme)
        return PoolObjects ?? new List<PoolObjectData>();
    }
}

[Serializable]
public class PoolObjectData
{
    public PoolKey Type;
    public bool IsUIObject;
    public GameObject Prefab;
    public int InitialSize = 5;
    public int MaxSize = 50;
}

[Serializable]

public enum PoolKey
{
    None = 0,

    Inventory_GamePad = 1,
    Inventory_Scrediver = 2,
    Inventory_Monitor = 3,
    Inventory_WineGlass = 4,
    Inventory_Knife = 5,
    Inventory_Book = 6,
    Inventory_Axe = 7,
    Inventory_Hammer = 8,
    Inventory_Bone = 9,
    Inventory_Teapot = 10,

    AttackItem = 100,

    Enemy_Basic = 150,
    Enemy_Warrior = 151,
    Enemy_Mage = 152,

    ParticleEffect_Hit = 200,
    ParticleEffect_Die = 201,
    ParticleEffect_Spawn = 202,
}