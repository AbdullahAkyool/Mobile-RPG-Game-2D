using System;
using UnityEngine;

/// <summary>
/// Static Event Manager - Grid sistemindeki event'leri yönetir
/// </summary>
public static class EventManager
{
    /// <summary>
    /// Item BasicGridController'dan CoordinateGridController'a taşındığında tetiklenir
    /// Parametreler: (fromGrid, toGrid, item)
    /// </summary>
    public static Action<BaseGridController, BaseGridController, InventoryItemController> OnItemTransferredToCoordinateGrid;

    /// <summary>
    /// BasicGridController temizlenmesi gerektiğinde tetiklenir
    /// </summary>
    public static Action OnBasicGridShouldClear;

    /// <summary>
    /// Yeni itemler spawn edilmesi gerektiğinde tetiklenir
    /// </summary>
    public static Action OnShouldSpawnNewItems;
}
