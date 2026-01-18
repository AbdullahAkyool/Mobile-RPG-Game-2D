using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private InventoryItemDatabaseSO itemDatabase;
    public InventoryItemDatabaseSO ItemDatabase => itemDatabase;

    [Header("Random Spawn Settings")]
    [SerializeField] private BasicGridController targetGrid;
    [SerializeField] private CoordinateGridController coordinateGrid;

    [Header("Index System")]
    [SerializeField] private int transferIndex = 0;
    private const int MAX_TRANSFER_INDEX = 3;

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
        // Event'leri dinle
        EventManager.OnItemTransferredToCoordinateGrid += OnItemTransferredToCoordinateGrid;
    }

    private void OnDisable()
    {
        // Event'leri kaldır
        EventManager.OnItemTransferredToCoordinateGrid -= OnItemTransferredToCoordinateGrid;
    }

    private void Start()
    {
        // Layout'un hesaplanması için bir frame bekle
        StartCoroutine(SpawnRandomItemsDelayed());
    }

    private IEnumerator SpawnRandomItemsDelayed()
    {
        yield return null; // Bir frame bekle (layout hesaplanması için)
        SpawnRandomItems();
    }

    public void SpawnRandomItems()
    {
        if (targetGrid == null) return;

        for (int i = 0; i < 3; i++)
        {
            if (!targetGrid.GetFirstEmptyCell(out var cell))
                return;

            PoolKey randomKey = (PoolKey)Random.Range(1, 10);
            
            var item = PoolManager.Instance.Spawn<InventoryItemController>(randomKey);
            
            if (item != null)
            {
                targetGrid.TryPlaceItem(item, cell);
            }
        }
    }

    private void OnItemTransferredToCoordinateGrid(BaseGridController fromGrid, BaseGridController toGrid, InventoryItemController item)
    {
        // Index'i artır
        transferIndex++;
        
        Debug.Log($"[GameManager] Item transferred. Index: {transferIndex}/{MAX_TRANSFER_INDEX}");

        // Index 3 olduğunda kontrol et
        if (transferIndex >= MAX_TRANSFER_INDEX)
        {
            CheckAndRespawnItems();
        }
    }

    private void CheckAndRespawnItems()
    {
        // Koşul kontrolü: CoordinateGridController'da en az 3 item olmalı
        if (coordinateGrid == null)
        {
            Debug.LogWarning("[GameManager] CoordinateGridController not assigned!");
            return;
        }

        int coordinateItemCount = coordinateGrid.GetUniqueItemCount();
        
        if (coordinateItemCount < 3)
        {
            Debug.Log($"[GameManager] Not enough items in CoordinateGrid ({coordinateItemCount} < 3). Waiting...");
            return;
        }

        // BasicGridController'da itemler varsa temizle
        if (targetGrid != null && targetGrid.GetItemCount() > 0)
        {
            Debug.Log("[GameManager] Clearing BasicGrid and respawning items...");
            targetGrid.ClearAllItems();
        }

        // Index'i sıfırla
        transferIndex = 0;

        // Yeni itemler spawn et
        SpawnRandomItems();
    }
}
