using UnityEngine;
using UnityEngine.UI;

public class InventoryItemView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image outlineImage;
    public Image OutlineImage => outlineImage;

    public void Initialize(InventoryItemModel model)
    {
        if (iconImage != null)
            iconImage.sprite = model.LevelData.itemIcon;
            outlineImage.sprite = model.LevelData.itemIcon;
    }

}
