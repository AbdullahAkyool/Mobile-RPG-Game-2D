using UnityEngine;

public class InventoryItemModel
{
    public InventoryItemData Data { get; }
    public ItemLevelData LevelData { get; private set; }
    public int Level { get; private set; }

    public InventoryItemModel(ItemType type, int level, InventoryItemDatabaseSO db)
    {
        Data = db.GetItem(type);
        SetLevel(level);
    }

    public void SetLevel(int level)
    {
        Level = level;
        LevelData = Data.itemLevels.Find(l => l.level == level);
    }

    public void GetDimensions(out int w, out int h)
    {
        var fp = Data.itemFootprintData;
        w = fp != null ? Mathf.Max(1, fp.width) : 1;
        h = fp != null ? Mathf.Max(1, fp.height) : 1;
    }

    public bool IsFilled(int x, int y)
    {
        var fp = Data.itemFootprintData;
        if (fp == null) return true;
        return fp.IsFilled(x, y);
    }
}
