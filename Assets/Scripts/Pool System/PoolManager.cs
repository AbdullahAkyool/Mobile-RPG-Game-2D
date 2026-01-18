using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PoolManager : MonoBehaviour //kendi pool sistemim
{
    public static PoolManager Instance { get; private set; }

    [Header("Parent Transforms")]
    [SerializeField] private Transform uiPoolParent;
    [SerializeField] private Transform worldPoolParent;

    // Pool storage - Enum keys
    private Dictionary<PoolKey, ObjectPool<IPoolable>> pools = new Dictionary<PoolKey, ObjectPool<IPoolable>>();

    // Default parent storage
    private Dictionary<PoolKey, Transform> defaultParents = new Dictionary<PoolKey, Transform>();

    [SerializeField] private PoolObjectDatabaseSO poolObjectDatabaseSO;

    void Awake()
    {
        // Singleton pattern - sadece bir instance olmalı
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        CreatePoolParents();
        InitializePools();
    }

    private void CreatePoolParents()
    {
        if (uiPoolParent == null)
        {
            GameObject uiParent = new GameObject("UI_Pool_Parent");
            uiPoolParent = uiParent.transform;
            uiPoolParent.SetParent(transform);
        }

        if (worldPoolParent == null)
        {
            GameObject worldParent = new GameObject("World_Pool_Parent");
            worldPoolParent = worldParent.transform;
            worldPoolParent.SetParent(transform);
        }
    }

    private void InitializePools()
    {
        // Veritabanı kontrolü
        if (poolObjectDatabaseSO == null)
        {
            Debug.LogError("[PoolManager] PoolObjectDatabaseSO is not assigned!");
            return;
        }

        // Group by enum type and create pools
        HashSet<PoolKey> processedTypes = new HashSet<PoolKey>();

        foreach (var poolData in poolObjectDatabaseSO.GetPoolObjectDataList())
        {
            if (poolData.Type == PoolKey.None)
            {
                Debug.LogWarning($"[PoolManager] Pool data has null Type!");
                continue;
            }

            // Skip if we already processed this enum type
            if (processedTypes.Contains(poolData.Type))
                continue;

            if (poolData.Prefab == null)
            {
                Debug.LogWarning($"[PoolManager] Pool data '{poolData.Type}' has null prefab!");
                continue;
            }

            CreatePool(poolData);
            processedTypes.Add(poolData.Type);
        }

        Debug.Log($"[PoolManager] Initialized {pools.Count} pools");
    }

    #region Pool Creation

    private void CreatePool(PoolObjectData poolData)
    {
        PoolKey key = poolData.Type;

        // Create dedicated parent for this pool
        string prefabName = poolData.Prefab.name;
        string parentName = poolData.IsUIObject
            ? $"{prefabName}_UIParent"
            : $"{prefabName}_WorldParent";

        GameObject poolParentObj = new GameObject(parentName);
        Transform poolParent = poolParentObj.transform;

        // Set parent to main UI or World parent
        if (poolData.IsUIObject)
        {
            poolParent.SetParent(uiPoolParent, false);
        }
        else
        {
            poolParent.SetParent(worldPoolParent, true);
        }

        // Store this as default parent for this pool
        defaultParents[key] = poolParent;

        // Get size settings from data
        int initialSize = Mathf.Max(1, poolData.InitialSize);
        int maxSize = poolData.MaxSize > 0 ? poolData.MaxSize : 100;

        // Create pool
        ObjectPool<IPoolable> pool = new ObjectPool<IPoolable>(
            createFunc: () => CreatePooledObject(poolData, poolParent),
            actionOnGet: OnGetFromPool,
            actionOnRelease: OnReleaseToPool,
            actionOnDestroy: OnDestroyPooledObject,
            collectionCheck: true,
            defaultCapacity: initialSize,
            maxSize: maxSize
        );

        pools[key] = pool;

        // Pre-warm pool
        List<IPoolable> temp = new List<IPoolable>();
        for (int i = 0; i < initialSize; i++)
        {
            temp.Add(pool.Get());
        }
        foreach (var obj in temp)
        {
            pool.Release(obj);
        }
    }

    private IPoolable CreatePooledObject(PoolObjectData poolData, Transform parent)
    {
        GameObject obj = Instantiate(poolData.Prefab, parent);
        obj.name = poolData.Prefab.name;

        IPoolable poolable = obj.GetComponent<IPoolable>();
        if (poolable == null)
        {
            Debug.LogError($"[PoolManager] Prefab '{poolData.Prefab.name}' does not have IPoolable component!");
            Destroy(obj);
            return null;
        }

        // Set inactive by default
        obj.SetActive(false);

        return poolable;
    }

    #endregion

    #region Pool Callbacks

    private void OnGetFromPool(IPoolable poolable)
    {
        poolable?.OnSpawn();
    }

    private void OnReleaseToPool(IPoolable poolable)
    {
        poolable?.OnDespawn();
    }

    private void OnDestroyPooledObject(IPoolable poolable)
    {
        if (poolable != null)
        {
            Component component = poolable as Component;
            if (component != null)
            {
                Destroy(component.gameObject);
            }
        }
    }

    #endregion

    #region Spawn/Despawn

    /// <summary>
    /// Spawn object from pool
    /// </summary>
    public T Spawn<T>(PoolKey poolKey) where T : Component, IPoolable
    {
        if (!pools.TryGetValue(poolKey, out ObjectPool<IPoolable> pool))
        {
            Debug.LogError($"[PoolManager] Pool '{poolKey}' not found!");
            return null;
        }

        IPoolable poolable = pool.Get();
        return poolable as T;
    }

    /// <summary>
    /// Spawn object at position with rotation
    /// </summary>
    public T Spawn<T>(PoolKey poolKey, Vector3 position, Quaternion rotation) where T : Component, IPoolable
    {
        T obj = Spawn<T>(poolKey);
        if (obj != null)
        {
            obj.transform.position = position;
            obj.transform.rotation = rotation;
        }
        return obj;
    }

    /// <summary>
    /// Spawn object at position
    /// </summary>
    public T Spawn<T>(PoolKey poolKey, Vector3 position) where T : Component, IPoolable
    {
        return Spawn<T>(poolKey, position, Quaternion.identity);
    }

    /// <summary>
    /// Spawn object with parent
    /// </summary>
    public T Spawn<T>(PoolKey poolKey, Transform parent) where T : Component, IPoolable
    {
        T obj = Spawn<T>(poolKey);
        if (obj != null)
        {
            obj.transform.SetParent(parent, false);
        }
        return obj;
    }

    /// <summary>
    /// Despawn object back to pool
    /// </summary>
    public void Despawn(PoolKey poolKey, IPoolable poolable)
    {
        if (poolable == null) return;

        if (!pools.TryGetValue(poolKey, out ObjectPool<IPoolable> pool))
        {
            Debug.LogError($"[PoolManager] Pool '{poolKey}' not found for despawn!");
            return;
        }

        // Reset parent to default pool parent
        if (defaultParents.TryGetValue(poolKey, out Transform parent))
        {
            Component component = poolable as Component;
            if (component != null)
            {
                component.transform.SetParent(parent);
            }
        }

        pool.Release(poolable);
    }

    /// <summary>
    /// Despawn with delay
    /// </summary>
    public void Despawn(PoolKey poolKey, IPoolable poolable, float delay)
    {
        if (delay <= 0)
        {
            Despawn(poolKey, poolable);
        }
        else
        {
            StartCoroutine(DespawnDelayed(poolKey, poolable, delay));
        }
    }

    private System.Collections.IEnumerator DespawnDelayed(PoolKey poolKey, IPoolable poolable, float delay)
    {
        yield return new WaitForSeconds(delay);
        Despawn(poolKey, poolable);
    }

    #endregion

    #region Utility

    /// <summary>
    /// Check if pool exists
    /// </summary>
    public bool HasPool(PoolKey poolKey)
    {
        return pools.ContainsKey(poolKey);
    }

    /// <summary>
    /// Get pool statistics
    /// </summary>
    public void LogPoolStats()
    {
        Debug.Log($"[PoolManager] Active Pools: {pools.Count}");
        foreach (var kvp in pools)
        {
            Debug.Log($"  - {kvp.Key}: Active/Inactive counts");
        }
    }

    #endregion
}
