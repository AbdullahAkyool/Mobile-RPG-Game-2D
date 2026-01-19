using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private InventoryItemDatabaseSO itemDatabase;
    public InventoryItemDatabaseSO ItemDatabase => itemDatabase;

    [Header("Random Item Spawn Settings")]
    [SerializeField] private BasicGridController targetGrid;
    [SerializeField] private CoordinateGridController coordinateGrid;
    [SerializeField] private int transferIndex = 0;
    private const int MAX_TRANSFER_INDEX = 3;

    [Header("Enemy Settings")]
    [SerializeField] private List<EnemyController> activeEnemies = new List<EnemyController>();
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private PoolKey defaultEnemyPoolKey = PoolKey.Enemy_Basic;

    public List<EnemyController> ActiveEnemies => activeEnemies;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        EventManager.OnItemTransferredToCoordinateGrid += OnItemTransferredToCoordinateGrid;
    }

    private void OnDisable()
    {
        EventManager.OnItemTransferredToCoordinateGrid -= OnItemTransferredToCoordinateGrid;
    }

    private void Start()
    {
        StartCoroutine(SpawnRandomItemsDelayed());
        StartCoroutine(SpawnInitialEnemyDelayed());
    }

    private IEnumerator SpawnInitialEnemyDelayed()
    {
        yield return null;
        SpawnEnemy();
    }

    #region Inventory Item Spawning
    private IEnumerator SpawnRandomItemsDelayed()
    {
        yield return null;
        SpawnRandomItems();
    }

    public void SpawnRandomItems()
    {
        if (targetGrid == null) return;

        List<PoolKey> available = new();

        // PoolKey aralığını sana göre ayarlayabilirsin
        for (int i = 1; i <= 10; i++)
            available.Add((PoolKey)i);

        // Shuffle
        for (int i = 0; i < available.Count; i++)
        {
            int r = Random.Range(i, available.Count);
            (available[i], available[r]) = (available[r], available[i]);
        }

        // İlk 3 benzersiz item
        for (int i = 0; i < 3; i++)
        {
            if (!targetGrid.GetFirstEmptyCell(out var cell))
                return;

            var key = available[i];
            var item = PoolManager.Instance.Spawn<InventoryItemController>(key);
            targetGrid.TryPlaceItem(item, cell);
        }
    }

    private void OnItemTransferredToCoordinateGrid(BaseGridController fromGrid, BaseGridController toGrid, InventoryItemController item)
    {
        transferIndex++;

        if (transferIndex >= MAX_TRANSFER_INDEX)
        {
            CheckAndRespawnItems();
        }
    }

    private void CheckAndRespawnItems()
    {
        if (coordinateGrid == null)
        {
            Debug.LogWarning("[GameManager] CoordinateGridController not assigned!");
            return;
        }

        int coordinateItemCount = coordinateGrid.GetUniqueItemCount();

        if (coordinateItemCount < 3)
        {
            return;
        }

        // BasicGridController'da itemler varsa temizle
        if (targetGrid != null && targetGrid.GetItemCount() > 0)
        {
            targetGrid.ClearAllItems();
        }

        transferIndex = 0;

        SpawnRandomItems();
    }

    #endregion

    #region Enemy Management

    [Header("Enemy Respawn Settings")]
    [SerializeField] private float enemyRespawnDelay = 3f;

    public void SpawnEnemy()
    {
        if (PoolManager.Instance == null)
        {
            Debug.LogError("[GameManager] PoolManager instance not found!");
            return;
        }

        EnemyController enemy = PoolManager.Instance.Spawn<EnemyController>(defaultEnemyPoolKey);
        if (enemy == null)
        {
            Debug.LogError($"[GameManager] Failed to spawn enemy from pool! PoolKey: {defaultEnemyPoolKey}");
            return;
        }

        if (enemySpawnPoint != null)
        {
            enemy.transform.position = enemySpawnPoint.position;
            enemy.transform.rotation = enemySpawnPoint.rotation;
        }

        ParticleEffectController spawnParticle = PoolManager.Instance.Spawn<ParticleEffectController>(PoolKey.ParticleEffect_Spawn);
        if (spawnParticle != null)
        {
            Vector3 pos = enemy.transform.position;
            pos.y -= .7f;
            spawnParticle.transform.position = pos;
        }

        if (!activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
        }

        EventManager.OnEnemySpawned?.Invoke(enemy);
    }

    public EnemyController GetRandomEnemy()
    {
        if (activeEnemies == null || activeEnemies.Count == 0) return null;

        int index = Random.Range(0, activeEnemies.Count);
        return activeEnemies[index];
    }

    public void OnEnemyDied(EnemyController enemy)
    {
        if (enemy == null) return;

        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }

        EventManager.OnEnemyDied?.Invoke(enemy);

        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Despawn(enemy.PoolKey, enemy);
        }

        StartCoroutine(SpawnNewEnemyAfterDelay(enemyRespawnDelay));
    }

    private IEnumerator SpawnNewEnemyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnEnemy();
    }

    #endregion
}
