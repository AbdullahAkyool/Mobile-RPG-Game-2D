using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BasicGridController : BaseGridController
{
    [Header("---- Grid Settings ----")]
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;
    [SerializeField] private Transform cellsRoot;

    // Doluluk
    private InventoryItemController[] occupied;
    private readonly Dictionary<int, GridCellController> cells = new();

    // Hücre bilgileri
    private int totalCells;
    private float cellWidth;
    private float cellHeight;
    private float spacing;

    private RectTransform gridRectTransform;

    // Preview renkleri
    private readonly Color previewValid = new(0.2f, 0.9f, 0.2f, 1f);
    private readonly Color previewInvalid = new(0.95f, 0.25f, 0.25f, 1f);

    private readonly List<GridCellController> previewCells = new();

    private void Awake()
    {
        if (horizontalLayout == null)
            horizontalLayout = GetComponent<HorizontalLayoutGroup>();

        if (cellsRoot == null)
            cellsRoot = transform;

        gridRectTransform = GetComponent<RectTransform>();

        EnsureItemsRoot();
        CacheLayout();
        RebuildOccupancy();
        AssignCells();
    }

    private void CacheLayout() // Layout'u Önbelleğe Al
    {
        spacing = horizontalLayout != null ? horizontalLayout.spacing : 0f;

        // Hücre sayısı
        totalCells = 0;
        for (int i = 0; i < cellsRoot.childCount; i++)
            if (cellsRoot.GetChild(i).GetComponent<GridCellController>() != null)
                totalCells++;

        if (totalCells <= 0) totalCells = 1;

        // Hücre boyutu
        if (cellsRoot.childCount > 0)
        {
            var rt = cellsRoot.GetChild(0).GetComponent<RectTransform>();
            cellWidth = rt.rect.width;
            cellHeight = rt.rect.height;
        }
        else
        {
            cellWidth = 100f;
            cellHeight = 100f;
        }
    }

    private void RebuildOccupancy() // Doluluk Dizisini Yeniden Oluştur
    {
        occupied = new InventoryItemController[totalCells];
    }

    private void AssignCells() // Hücreleri Atama
    {
        cells.Clear();
        int index = 0;

        for (int i = 0; i < cellsRoot.childCount; i++)
        {
            var child = cellsRoot.GetChild(i);
            var cell = child.GetComponent<GridCellController>();
            if (cell == null) continue;

            cell.SetGrid(this, ToCell(index)); // BaseGridController ile uyumlu
            cells[index] = cell;

            index++;
        }
    }

    // =========================================================
    // HELPERS
    // =========================================================
    private int ToIndex(Vector2Int cell) => Mathf.Clamp(cell.x, 0, totalCells - 1);
    private Vector2Int ToCell(int index) => new(index, 0);

    public Vector2 GetCellSize()
    {
        return new Vector2(cellWidth, cellHeight);
    }

    public bool GetFirstEmptyCell(out Vector2Int cell)
    {
        for (int i = 0; i < totalCells; i++)
        {
            if (occupied[i] == null)
            {
                cell = ToCell(i);
                return true;
            }
        }
        cell = default;
        return false;
    }


    // =========================================================
    // BASE OVERRIDES
    // =========================================================

    public override bool TryPlaceItem(InventoryItemController item, Vector2Int anchorCell)
    {
        if (anchorCell.y != 0) return false;
        if (anchorCell.x < 0 || anchorCell.x >= totalCells) return false;

        int index = anchorCell.x;
        if (occupied[index] != null) return false;

        occupied[index] = item;
        ApplyItemVisual(item, index);
        item.Placement.SetCurrentPlacement(this, ToCell(index));
        EventManager.OnItemPlaced?.Invoke(this, item);
        return true;
    }

    public override void RemoveItem(InventoryItemController item)
    {
        for (int i = 0; i < occupied.Length; i++)
        {
            if (occupied[i] == item)
            {
                occupied[i] = null;
                break;
            }
        }
        item.Placement.ClearCurrentPlacement(this);
        EventManager.OnItemRemoved?.Invoke(this, item);
    }

    public override void PreviewItemPlacement(InventoryItemController item, Vector2Int anchorCell)
    {
        ClearPreview();

        int index = ToIndex(anchorCell);
        if (!cells.TryGetValue(index, out var cell)) return;

        previewCells.Add(cell);

        bool blocked = occupied[index] != null;
        cell.SetColor(blocked ? previewInvalid : previewValid);
    }

    public override void ClearPreview()
    {
        foreach (var c in previewCells)
            c?.ResetColor();
        previewCells.Clear();
    }

    public override bool TryScreenPointToCell(Vector2 screenPoint, Camera eventCamera, out Vector2Int cell)
    {
        cell = default;

        if (gridRectTransform == null) return false;

        // ✅ rect dışındaysa cell yok
        if (!RectTransformUtility.RectangleContainsScreenPoint(gridRectTransform, screenPoint, eventCamera))
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(gridRectTransform, screenPoint, eventCamera, out var local))
            return false;

        float startX = -gridRectTransform.rect.width * 0.5f + horizontalLayout.padding.left + cellWidth * 0.5f;
        float stepX = cellWidth + spacing;

        int index = Mathf.FloorToInt((local.x - startX) / stepX);

        // içerideyiz ama index yine de taşabilir → clamp
        index = Mathf.Clamp(index, 0, totalCells - 1);

        cell = ToCell(index);
        return true;
    }


    public override RectTransform GetGridRectTransform() => gridRectTransform; // Grid'in RectTransform'ini al

    /// <summary>
    /// Grid'deki tüm itemleri temizler ve pool'a geri gönderir
    /// </summary>
    public void ClearAllItems()
    {
        if (occupied == null) return;

        for (int i = 0; i < occupied.Length; i++)
        {
            if (occupied[i] != null)
            {
                var item = occupied[i];
                occupied[i] = null;
                item.Placement.ClearCurrentPlacement(this);

                // Pool'a geri gönder
                if (PoolManager.Instance != null)
                {
                    PoolManager.Instance.Despawn(item.PoolKey, item);
                }
            }
        }
    }

    /// <summary>
    /// Grid'deki item sayısını döndürür
    /// </summary>
    public int GetItemCount()
    {
        if (occupied == null) return 0;

        int count = 0;
        for (int i = 0; i < occupied.Length; i++)
        {
            if (occupied[i] != null) count++;
        }
        return count;
    }

    private void ApplyItemVisual(InventoryItemController item, int index) // itemi hücreye yerleştir
    {
        var rt = item.GetComponent<RectTransform>();
        rt.SetParent(itemsRoot, false);

        rt.sizeDelta = new(cellWidth, cellHeight);
        rt.anchorMin = rt.anchorMax = rt.pivot = new(0.5f, 0.5f);

        // Layout'u force rebuild et (eğer henüz hesaplanmamışsa)
        if (horizontalLayout != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridRectTransform);
        }

        // Cell pozisyonunu hesapla
        Vector3 targetPosition;
        if (cells.TryGetValue(index, out var cell))
        {
            var cellRt = cell.GetComponent<RectTransform>();
            // Cell'in world position'ını al ve itemsRoot'un local space'ine çevir
            // itemsRoot ve grid aynı parent'ta olduğu için bu çalışır
            Vector3 worldPos = cellRt.position;
            targetPosition = itemsRoot.InverseTransformPoint(worldPos);
        }
        else
        {
            // Fallback: Pozisyonu manuel hesapla (cell bulunamazsa)
            float paddingLeft = horizontalLayout != null ? horizontalLayout.padding.left : 0f;
            float startX = -gridRectTransform.rect.width * 0.5f + paddingLeft + cellWidth * 0.5f;
            float xPos = startX + index * (cellWidth + spacing);
            // Grid'in local space'inde pozisyonu hesapla, sonra itemsRoot'a çevir
            Vector3 gridLocalPos = new Vector3(xPos, 0f, 0f);
            Vector3 worldPos = gridRectTransform.TransformPoint(gridLocalPos);
            targetPosition = itemsRoot.InverseTransformPoint(worldPos);
        }

        rt.localPosition = targetPosition;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    private void EnsureItemsRoot() // Items Root'u Oluştur
    {
        if (itemsRoot != null) return;

        GameObject go = new($"{name}_ItemsRoot", typeof(RectTransform));
        itemsRoot = go.GetComponent<RectTransform>();
        itemsRoot.SetParent(transform.parent, false);
        itemsRoot.SetSiblingIndex(transform.GetSiblingIndex() + 1);

        var gridRt = GetComponent<RectTransform>();
        itemsRoot.anchorMin = gridRt.anchorMin;
        itemsRoot.anchorMax = gridRt.anchorMax;
        itemsRoot.pivot = gridRt.pivot;
        itemsRoot.offsetMin = gridRt.offsetMin;
        itemsRoot.offsetMax = gridRt.offsetMax;
        itemsRoot.localScale = Vector3.one;
        itemsRoot.localRotation = Quaternion.identity;
    }
}
