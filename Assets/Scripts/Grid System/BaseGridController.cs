using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tüm Grid Sistemlerinin Base Class'ı (Abstract)
/// Hem koordinat bazlı grid (tetris-style) hem de yığın grid (stack-style) bu class'tan türer.
/// </summary>
public abstract class BaseGridController : MonoBehaviour
{
    [Header("---- Grid Type ----")]
    [SerializeField] private GridType gridType = GridType.None;
    public GridType GridType => gridType;

    [Header("---- UI ----")]
    [SerializeField] protected RectTransform itemsRoot; // Öğelerin parentlanacağı root
    public RectTransform ItemsRoot => itemsRoot;

    protected virtual void OnEnable()
    {
        // GridManager'a kaydol
        if (GridManager.Instance != null)
        {
            GridManager.Instance.RegisterGrid(this);
        }
        else
        {
            Debug.LogWarning($"[BaseGridController] GridManager not found! Grid '{name}' cannot be registered.");
        }
    }

    protected virtual void OnDisable()
    {
        // GridManager'dan çık
        if (GridManager.Instance != null)
        {
            GridManager.Instance.UnregisterGrid(this);
        }
    }

    /// <summary>
    /// Öğeyi Grid'e Yerleştirmeyi Dene (Abstract)
    /// Her grid tipi kendi placement mantığını implement eder.
    /// </summary>
    public abstract bool TryPlaceItem(InventoryItem item, Vector2Int anchorCell);

    /// <summary>
    /// Öğeyi Grid'den Kaldır (Abstract)
    /// </summary>
    public abstract void RemoveItem(InventoryItem item);

    /// <summary>
    /// Yerleşim Önizlemesi Göster (Abstract)
    /// Sürükleme sırasında hücreleri renklendirir.
    /// </summary>
    public abstract void PreviewItemPlacement(InventoryItem item, Vector2Int anchorCell);

    /// <summary>
    /// Önizlemeyi Temizle (Abstract)
    /// </summary>
    public abstract void ClearPreview();

    /// <summary>
    /// Ekran Noktasını Hücreye Çevir (Abstract)
    /// Her grid tipi kendi koordinat sistemine göre hesaplar.
    /// </summary>
    public abstract bool TryScreenPointToCell(Vector2 screenPoint, Camera eventCamera, out Vector2Int cell);

    /// <summary>
    /// GridRectTransform'u Al
    /// GridManager'ın fare altındaki grid'i bulması için gerekli.
    /// </summary>
    public abstract RectTransform GetGridRectTransform();

    /// <summary>
    /// Grid'in Öğe Kabul Edip Etmediği (Opsiyonel Override)
    /// Bazı gridler sadece belirli item tiplerini kabul edebilir.
    /// </summary>
    public virtual bool CanAcceptItem(InventoryItem item)
    {
        return true; // Varsayılan: tüm item'ları kabul et
    }
}
