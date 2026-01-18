using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InventoryItemDatabaseSO", menuName = "ScriptableObjects/Inventory/Inventory Item Database")]
public class InventoryItemDatabaseSO : ScriptableObject
{
    public List<InventoryItemData> items = new List<InventoryItemData>();

    public InventoryItemData GetItem(ItemType type)
    {
        return items.Find(i => i.itemType == type);
    }

    public ItemLevelData GetItemProperitesByLevel(ItemType type, int level)
    {
        var item = GetItem(type);
        if (item == null) return null;

        return item.itemLevels.Find(l => l.level == level);
    }
}

[Serializable]
public class InventoryItemData
{
    [Header("---- Item Data ----")]
    public ItemType itemType;
    public List<ItemLevelData> itemLevels = new List<ItemLevelData>();

    [Header("---- Footprint ----")]
    public ItemFootprint itemFootprintData;
}

[Serializable]
public class ItemLevelData
{
    public int level;
    public int damagePower;
    public float cooldownTime;
    public Sprite itemIcon;
}

[Serializable]
public class ItemFootprint
{
    [Min(1)] public int width = 1;
    [Min(1)] public int height = 1;
    [SerializeField] private bool[] filled;

    public int CellCount => Mathf.Max(1, width) * Mathf.Max(1, height);

    public bool HasValidArray => filled != null && filled.Length == CellCount;

    public void EnsureFilledArray(bool defaultFill = true)
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);

        int expected = width * height;
        if (filled == null || filled.Length != expected)
        {
            filled = new bool[expected];
            if (defaultFill)
            {
                for (int i = 0; i < expected; i++) filled[i] = true;
            }
        }
    }

    public bool IsFilled(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return false;

        // Array yoksa “tam dikdörtgen” varsayımı
        if (!HasValidArray) return true;

        return filled[y * width + x];
    }

    public void SetFilled(int x, int y, bool value)
    {
        EnsureFilledArray();
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        filled[y * width + x] = value;
    }

    public void FillAll(bool value)
    {
        EnsureFilledArray();
        for (int i = 0; i < filled.Length; i++) filled[i] = value;
    }

    public bool[] GetFilledCopy()
    {
        if (!HasValidArray) return null;
        var copy = new bool[filled.Length];
        Array.Copy(filled, copy, filled.Length);
        return copy;
    }

    public void SetFilledUnsafe(bool[] newFilled) => filled = newFilled;

    /// İstersen footprint'i tamamen dikdörtgene çevirir
    public void MakeRect()
    {
        EnsureFilledArray(defaultFill: true);
    }
}
