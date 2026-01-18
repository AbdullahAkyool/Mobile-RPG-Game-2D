using System.Collections.Generic;
using UnityEngine;

// Sahnedeki tüm BaseGridController'ları merkezi olarak yönetir.
// Fare altındaki grid'i bulmak, type bazlı erişim gibi işlemlerden sorumlu.
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    private readonly HashSet<BaseGridController> allGrids = new(); // Tüm aktif grid'leri tutar
    private readonly Dictionary<GridType, BaseGridController> gridsByType = new(); // Type bazlı erişim için

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // GridController nesnesi aktif olduğunda kendini buraya kaydeder.
    public void RegisterGrid(BaseGridController grid)
    {
        if (grid == null) return;

        allGrids.Add(grid);

        // Type'ı varsa dictionary'ye de ekle
        if (grid.GridType != GridType.None)
        {
            if (gridsByType.ContainsKey(grid.GridType))
            {
                Debug.LogWarning($"[GridManager] Grid type '{grid.GridType}' already exists! Overwriting...");
            }
            gridsByType[grid.GridType] = grid;
        }

        Debug.Log($"[GridManager] Registered grid: {grid.name} (Type: {grid.GridType})");
    }

    // GridController nesnesi devre dışı kaldığında kendini buradan siler.
    public void UnregisterGrid(BaseGridController grid)
    {
        if (grid == null) return;

        allGrids.Remove(grid);

        if (grid.GridType != GridType.None && gridsByType.ContainsKey(grid.GridType))
        {
            gridsByType.Remove(grid.GridType);
        }

        Debug.Log($"[GridManager] Unregistered grid: {grid.name}");
    }

    /// Fare Altındaki Grid'i Bul
    /// Ekran noktasının hangi grid içinde olduğunu bulur.
    public BaseGridController FindGridUnderPointer(Vector2 screenPoint, Camera eventCamera)
    {
        // Canvas sort order veya raycast priority'ye göre sıralama eklenebilir
        foreach (var grid in allGrids)
        {
            if (grid == null || !grid.enabled) continue;
            if (IsScreenPointInsideGridRect(grid, screenPoint, eventCamera))
                return grid;
        }
        return null;
    }

    /// Type Bazlı Grid Erişimi
    /// Belirli bir type'daki grid'e doğrudan erişim.
    public BaseGridController GetGridByType(GridType type)
    {
        gridsByType.TryGetValue(type, out var grid);
        return grid;
    }

    public IEnumerable<BaseGridController> GetAllGrids()
    {
        return allGrids;
    }

    /// Ekran Noktası Grid İçinde mi
    private bool IsScreenPointInsideGridRect(BaseGridController grid, Vector2 screenPoint, Camera eventCamera)
    {
        var gridRt = grid.GetGridRectTransform();
        if (gridRt == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(gridRt, screenPoint, eventCamera);
    }
}

public enum GridType
{
    None = 0,
    PlayerInventory = 1,
    PossibleItems = 2,  // BasicGridController kullanır - her item 1 hücre kaplar
}
