using UnityEngine;

public class InventoryItemPlacement : MonoBehaviour
{
    public InventoryItemModel Model { get; private set; }

    public BaseGridController CurrentGrid { get; private set; }
    public Vector2Int CurrentAnchor { get; private set; }

    private BaseGridController revertGrid;
    public BaseGridController RevertGrid => revertGrid; // Event tetikleme için erişim
    private Vector2Int revertAnchor;
    private Transform revertParent;

    public void Initialize(InventoryItemModel model)
    {
        Model = model;
    }

    public void BeginDrag(InventoryItemController owner)
    {
        // revert snapshot
        revertGrid = CurrentGrid;
        revertAnchor = CurrentAnchor;
        revertParent = owner.transform.parent;

        // grid occupancy’den çıkar
        if (CurrentGrid != null)
            CurrentGrid.RemoveItem(owner);
    }

    public bool TryPlace(InventoryItemController owner, BaseGridController grid, Vector2Int anchor)
    {
        if (grid == null) return false;

        bool placed = grid.TryPlaceItem(owner, anchor);
        if (placed) return true;

        return false;
    }

    public void Revert(InventoryItemController owner)
    {
        if (revertGrid != null)
        {
            bool ok = revertGrid.TryPlaceItem(owner, revertAnchor);
            if (ok) return;
        }

        // revert bile yoksa / başarısızsa eski parent’a dön
        owner.transform.SetParent(revertParent, false);
    }

    // grids call these
    public void SetCurrentPlacement(BaseGridController grid, Vector2Int anchor)
    {
        CurrentGrid = grid;
        CurrentAnchor = anchor;
    }

    public void ClearCurrentPlacement(BaseGridController grid)
    {
        // grid null geçilebilir (begin drag temizlik vb.)
        if (grid != null && CurrentGrid != grid) return;

        CurrentGrid = null;
        CurrentAnchor = default;
    }
}
