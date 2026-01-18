using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IPoolable, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Item Info")]
    [SerializeField] private ItemType itemType;
    private InventoryItemData inventoryItemData;
    private int itemLevel = 0;
    public int ItemLevel { get => itemLevel; set { itemLevel = value; } }

    [Header("Refs")]
    [SerializeField] private Image itemImage;
    [SerializeField] private Image itemOutline;
    private RectTransform rectTransform;

    [Tooltip("Drag sırasında referans alınan nokta. Null ise itemImage veya kendi RectTransform kullanılır.")]
    [SerializeField] private RectTransform grabReference;

    // Placement state
    private BaseGridController currentGrid;
    private Vector2Int currentAnchor;

    // Revert state
    private BaseGridController revertGrid;
    private Vector2Int revertAnchor;
    private Transform revertParent;

    // Drag visuals
    private Vector2 originalPivot;
    private Vector2 originalAnchorMin;
    private Vector2 originalAnchorMax;

    // Hover/preview state
    private BaseGridController hoverGrid;
    private Vector2Int hoverAnchor;
    private bool hasHover;

    // Sizing
    private BaseGridController lastSizingGrid;

    // Drag parent (Canvas)
    private Transform dragRoot;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (grabReference == null && itemImage != null) grabReference = itemImage.rectTransform;
        if (grabReference == null) grabReference = rectTransform;
    }

    void Start()
    {
        SetItem();
    }

    public void OnSpawn()
    {
        gameObject.SetActive(true);
    }

    public void OnDespawn()
    {
        gameObject.SetActive(false);
    }

    public void SetItem()
    {
        inventoryItemData = GameManager.Instance.ItemDatabase.GetItem(itemType);
        ItemLevelData itemLevelData = inventoryItemData.itemLevels.Find(l => l.level == itemLevel);
        itemImage.sprite = itemLevelData.itemIcon;
        itemOutline.sprite = itemLevelData.itemIcon;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Save rect settings
        originalPivot = rectTransform.pivot;
        originalAnchorMin = rectTransform.anchorMin;
        originalAnchorMax = rectTransform.anchorMax;

        // Drag pivot center for better feel
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // Save revert info
        revertGrid = currentGrid;
        revertAnchor = currentAnchor;
        revertParent = transform.parent;

        // Remove from grid occupancy during drag
        if (currentGrid != null)
            currentGrid.RemoveItem(this);

        // Find top canvas to drag under (prevents scale issues)
        dragRoot = FindTopCanvasTransform();
        transform.SetParent(dragRoot != null ? dragRoot : transform.root, true);
        transform.SetAsLastSibling();

        if (itemImage != null)
            itemImage.raycastTarget = false;

        // Clear hover states
        ClearHover();
        lastSizingGrid = null;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Move item to pointer (UI-safe)
        rectTransform.position = eventData.position;

        var grid = GridManager.Instance?.FindGridUnderPointer(eventData.position, eventData.pressEventCamera);

        if (grid != null && grid.TryScreenPointToCell(eventData.position, eventData.pressEventCamera, out var pointerCell))
        {
            var anchor = ComputeAnchorFromPointerCell(grid, pointerCell, eventData);

            // sizing only when grid changes
            if (lastSizingGrid != grid)
            {
                ApplyDragSizingForGrid(grid);
                lastSizingGrid = grid;
            }

            // update preview
            if (hoverGrid != null && hoverGrid != grid)
                hoverGrid.ClearPreview();

            hoverGrid = grid;
            hoverAnchor = anchor;
            hasHover = true;

            hoverGrid.PreviewItemPlacement(this, hoverAnchor);
            return;
        }

        // Not on valid cell -> clear preview
        ClearHover();
        lastSizingGrid = null;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        bool placed = false;

        if (hasHover && hoverGrid != null)
        {
            hoverGrid.ClearPreview();
            placed = hoverGrid.TryPlaceItem(this, hoverAnchor);
        }

        if (!placed)
        {
            // revert to previous placement
            if (revertGrid != null)
            {
                revertGrid.ClearPreview();
                placed = revertGrid.TryPlaceItem(this, revertAnchor);
            }
        }

        // If still not placed: just go back to previous parent (free floating)
        var targetParent = placed
            ? (currentGrid != null && currentGrid.ItemsRoot != null ? currentGrid.ItemsRoot : (currentGrid != null ? currentGrid.transform : revertParent))
            : revertParent;

        transform.SetParent(targetParent, false);

        if (itemImage != null)
            itemImage.raycastTarget = true;

        ClearHover();

        // If NOT placed, restore original rect settings (placed items are positioned by grid)
        if (!placed)
        {
            rectTransform.pivot = originalPivot;
            rectTransform.anchorMin = originalAnchorMin;
            rectTransform.anchorMax = originalAnchorMax;
        }
    }

    // =========================================================
    // Sizing
    // =========================================================

    private void ApplyDragSizingForGrid(BaseGridController grid)
    {
        if (rectTransform == null) return;

        if (grid is CoordinateGridController coordinateGrid)
        {
            coordinateGrid.GetLayoutMetrics(out var cellSize, out var spacing);
            GetDimensions(out var w, out var h);

            float pxW = w * cellSize.x + Mathf.Max(0, w - 1) * spacing.x;
            float pxH = h * cellSize.y + Mathf.Max(0, h - 1) * spacing.y;

            rectTransform.sizeDelta = new Vector2(pxW, pxH);
        }
        else if (grid is BasicGridController basicGrid)
        {
            rectTransform.sizeDelta = basicGrid.GetCellSize();
        }
    }

    // =========================================================
    // Anchor computation
    // =========================================================

    private Vector2Int ComputeAnchorFromPointerCell(BaseGridController grid, Vector2Int pointerCell, PointerEventData eventData)
    {
        // BasicGrid: always 1 cell footprint
        if (grid is not CoordinateGridController coordinateGrid)
            return pointerCell;

        coordinateGrid.GetLayoutMetrics(out var cellSize, out var spacing);
        GetDimensions(out var w, out var h);

        float stepX = Mathf.Max(1f, cellSize.x + spacing.x);
        float stepY = Mathf.Max(1f, cellSize.y + spacing.y);

        var reference = grabReference != null ? grabReference : rectTransform;

        if (reference != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(reference, eventData.position, eventData.pressEventCamera, out var local))
        {
            var rect = reference.rect;
            var topLeft = new Vector2(-rect.width * reference.pivot.x, rect.height * (1f - reference.pivot.y));
            var fromTopLeft = local - topLeft;

            int localX = Mathf.Clamp(Mathf.FloorToInt(fromTopLeft.x / stepX), 0, Mathf.Max(0, w - 1));
            int localY = Mathf.Clamp(Mathf.FloorToInt(fromTopLeft.y / stepY), 0, Mathf.Max(0, h - 1));

            return pointerCell - new Vector2Int(localX, localY);
        }

        // fallback: assume center grab
        int offX = Mathf.Clamp(w / 2, 0, Mathf.Max(0, w - 1));
        int offY = Mathf.Clamp(h / 2, 0, Mathf.Max(0, h - 1));
        return pointerCell - new Vector2Int(offX, offY);
    }

    // =========================================================
    // Footprint helpers
    // =========================================================

    public void GetDimensions(out int w, out int h)
    {
        ItemFootprint footprintData = inventoryItemData.itemFootprintData;

        w = footprintData != null ? Mathf.Max(1, footprintData.width) : 1;
        h = footprintData != null ? Mathf.Max(1, footprintData.height) : 1;
    }

    public bool IsFilledAtLocal(int localX, int localY)
    {
        ItemFootprint footprintData = inventoryItemData.itemFootprintData;

        if (footprintData == null) return true;
        return footprintData.IsFilled(localX, localY);
    }

    // =========================================================
    // Placement API used by grids
    // =========================================================

    public void SetCurrentPlacement(BaseGridController grid, Vector2Int anchorCell)
    {
        currentGrid = grid;
        currentAnchor = anchorCell;
    }

    public void ClearCurrentPlacement(BaseGridController grid)
    {
        if (currentGrid != grid) return;
        currentGrid = null;
        currentAnchor = default;
    }

    // =========================================================

    private void ClearHover()
    {
        if (hoverGrid != null)
            hoverGrid.ClearPreview();

        hoverGrid = null;
        hasHover = false;
    }

    private Transform FindTopCanvasTransform()
    {
        // Find nearest canvas up the hierarchy; then climb to its root
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        return canvas.rootCanvas != null ? canvas.rootCanvas.transform : canvas.transform;
    }
}
