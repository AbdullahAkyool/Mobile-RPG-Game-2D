using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PoolObjectDatabaseSO", menuName = "ScriptableObjects/Pool System/PoolObjectDatabaseSO", order = 1)]
public class PoolObjectDatabaseSO : ScriptableObject
{
    [SerializeField] private List<PoolObjectData> PoolObjects;

    public PoolObjectData GetPoolObjectData(PoolKey type)
    {
        return PoolObjects.Find(po => po.Type == type);
    }

    public List<PoolObjectData> GetPoolObjectDataList()
    {
        return PoolObjects;
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
    TestA = 1,
    TestB = 2,

}