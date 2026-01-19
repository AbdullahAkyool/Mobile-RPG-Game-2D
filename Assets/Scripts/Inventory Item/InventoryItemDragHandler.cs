using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(InventoryItemPlacement))]
public class InventoryItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image itemImage;
    [SerializeField] private RectTransform grabReference;

    private RectTransform rect;
    private InventoryItemController owner;
    private InventoryItemPlacement placement;
    private InventoryItemModel model;

    private Vector2 originalPivot;
    private Vector2 originalAnchorMin;
    private Vector2 originalAnchorMax;

    private BaseGridController hoverGrid;
    private Vector2Int hoverAnchor;
    private bool hasHover;

    private BaseGridController lastSizingGrid;
    private Transform dragRoot;

    public void Initialize(InventoryItemController owner, InventoryItemModel model, InventoryItemPlacement placement)
    {
        this.owner = owner;
        this.model = model;
        this.placement = placement;
        rect = GetComponent<RectTransform>();

        if (grabReference == null && itemImage != null) grabReference = itemImage.rectTransform;
        if (grabReference == null) grabReference = rect;
    }

    public void OnBeginDrag(PointerEventData e)
    {
        originalPivot = rect.pivot;
        originalAnchorMin = rect.anchorMin;
        originalAnchorMax = rect.anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);

        placement.BeginDrag(owner);

        dragRoot = FindTopCanvasTransform();
        transform.SetParent(dragRoot != null ? dragRoot : transform.root, true);
        transform.SetAsLastSibling();

        if (itemImage != null) itemImage.raycastTarget = false;

        ClearHover();
        lastSizingGrid = null;
    }

    public void OnDrag(PointerEventData e)
    {
        rect.position = e.position;

        var grid = GridManager.Instance?.FindGridUnderPointer(e.position, e.pressEventCamera);

        if (grid != null && grid.TryScreenPointToCell(e.position, e.pressEventCamera, out var pointerCell))
        {
            var anchor = ComputeAnchor(grid, pointerCell, e);

            if (lastSizingGrid != grid)
            {
                ApplySizing(grid);
                lastSizingGrid = grid;
            }

            if (hoverGrid != null && hoverGrid != grid) hoverGrid.ClearPreview();

            hoverGrid = grid;
            hoverAnchor = anchor;
            hasHover = true;

            hoverGrid.PreviewItemPlacement(owner, hoverAnchor);
            return;
        }

        ClearHover();
        lastSizingGrid = null;
    }

    public void OnEndDrag(PointerEventData e)
    {
        bool placed = false;
        BaseGridController placedGrid = null;

        if (hasHover && hoverGrid != null)
        {
            hoverGrid.ClearPreview();
            placed = placement.TryPlace(owner, hoverGrid, hoverAnchor);
            if (placed)
            {
                placedGrid = hoverGrid;
            }
        }

        if (!placed)
        {
            placement.Revert(owner);

            // UI ayarlarını geri al
            rect.pivot = originalPivot;
            rect.anchorMin = originalAnchorMin;
            rect.anchorMax = originalAnchorMax;
        }

        // Event tetikle: BasicGridController'dan CoordinateGridController'a transfer
        if (placed && placedGrid != null && placement.RevertGrid != null && placement.RevertGrid != placedGrid)
        {
            if (placement.RevertGrid is BasicGridController && placedGrid is CoordinateGridController)
            {
                EventManager.OnItemTransferredToCoordinateGrid?.Invoke(placement.RevertGrid, placedGrid, owner);
            }
        }

        if (itemImage != null) itemImage.raycastTarget = true;

        ClearHover();
    }

    private void ApplySizing(BaseGridController grid)
    {
        if (grid is CoordinateGridController coord)
        {
            coord.GetLayoutMetrics(out var cellSize, out var spacing);
            model.GetDimensions(out var w, out var h);

            float pxW = w * cellSize.x + Mathf.Max(0, w - 1) * spacing.x;
            float pxH = h * cellSize.y + Mathf.Max(0, h - 1) * spacing.y;

            rect.sizeDelta = new Vector2(pxW, pxH);
        }
        else if (grid is BasicGridController basic)
        {
            rect.sizeDelta = basic.GetCellSize();
        }
    }

    private Vector2Int ComputeAnchor(BaseGridController grid, Vector2Int pointerCell, PointerEventData e)
    {
        if (grid is not CoordinateGridController coord)
            return pointerCell;

        coord.GetLayoutMetrics(out var cellSize, out var spacing);
        model.GetDimensions(out var w, out var h);

        float stepX = Mathf.Max(1f, cellSize.x + spacing.x);
        float stepY = Mathf.Max(1f, cellSize.y + spacing.y);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(grabReference, e.position, e.pressEventCamera, out var local))
        {
            var r = grabReference.rect;
            var topLeft = new Vector2(-r.width * grabReference.pivot.x, r.height * (1f - grabReference.pivot.y));
            var fromTopLeft = local - topLeft;

            int lx = Mathf.Clamp(Mathf.FloorToInt(fromTopLeft.x / stepX), 0, Mathf.Max(0, w - 1));
            int ly = Mathf.Clamp(Mathf.FloorToInt(fromTopLeft.y / stepY), 0, Mathf.Max(0, h - 1));

            return pointerCell - new Vector2Int(lx, ly);
        }

        return pointerCell;
    }

    private void ClearHover()
    {
        if (hoverGrid != null) hoverGrid.ClearPreview();
        hoverGrid = null;
        hasHover = false;
    }

    private Transform FindTopCanvasTransform()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        return canvas.rootCanvas != null ? canvas.rootCanvas.transform : canvas.transform;
    }
}
