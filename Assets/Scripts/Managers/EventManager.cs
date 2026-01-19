using System;
using UnityEngine;

public static class EventManager
{
    public static Action<BaseGridController, InventoryItemController> OnItemPlaced;
    public static Action<BaseGridController, InventoryItemController> OnItemRemoved;

    public static Action<BaseGridController, BaseGridController, InventoryItemController> OnItemTransferredToCoordinateGrid; // fromGrid, toGrid, item

    public static Action OnBasicGridShouldClear;
    public static Action OnShouldSpawnNewItems;

    public static Action<AttackItemController> OnGetAttackItemForPlayer;
}
