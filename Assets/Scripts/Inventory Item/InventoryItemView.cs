using UnityEngine;
using UnityEngine.UI;

public class InventoryItemView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image outlineImage;
    [SerializeField] private Image hoverHighlight;
    [SerializeField] private Image selectedHighlight;
    [SerializeField] private Image cooldownOverlay;

    private Color outlineDefault = Color.white;
    private Color outlineHover = new Color(0.3f, 0.8f, 1f);
    private Color outlineSelected = Color.yellow;

    public void Initialize(InventoryItemModel model)
    {
        if (iconImage != null)
            iconImage.sprite = model.LevelData.itemIcon;

        HideAll();
    }

    public void ShowHover(bool active)
    {
        if (hoverHighlight) hoverHighlight.enabled = active;
        if (outlineImage) outlineImage.color = active ? outlineHover : outlineDefault;
    }

    public void ShowSelected(bool active)
    {
        if (selectedHighlight) selectedHighlight.enabled = active;
        if (outlineImage) outlineImage.color = active ? outlineSelected : outlineDefault;
    }

    public void SetCooldown(float normalized)
    {
        normalized = Mathf.Clamp01(normalized);
        if (!cooldownOverlay) return;

        cooldownOverlay.fillAmount = normalized;
        cooldownOverlay.enabled = normalized > 0;
    }

    public void HideAll()
    {
        if (hoverHighlight) hoverHighlight.enabled = false;
        if (selectedHighlight) selectedHighlight.enabled = false;
        if (cooldownOverlay) cooldownOverlay.enabled = false;

        if (outlineImage) outlineImage.color = outlineDefault;
    }
}
