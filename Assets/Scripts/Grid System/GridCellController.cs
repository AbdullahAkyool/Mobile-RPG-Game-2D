using UnityEngine;
using UnityEngine.UI;

public class GridCellController : MonoBehaviour
{
    private BaseGridController grid;
    public BaseGridController Grid => grid;
    private Vector2Int coordinate;
    public Vector2Int Coordinate => coordinate;

    private Image image;
    private Color baseColor;
    private bool baseColorCaptured;

    private void Awake()
    {
        image = GetComponent<Image>();
        if (image != null)
        {
            baseColor = image.color;
            baseColorCaptured = true;
        }
    }

    public void SetGrid(BaseGridController inventoryGrid, Vector2Int coord)
    {
        grid = inventoryGrid;
        coordinate = coord;
    }

    public void SetColor(Color color)
    {
        if (image == null) image = GetComponent<Image>();
        if (image == null) return;

        if (!baseColorCaptured)
        {
            baseColor = image.color;
            baseColorCaptured = true;
        }

        image.color = color;
    }

    public void ResetColor()
    {
        if (image == null) image = GetComponent<Image>();
        if (image == null) return;
        if (!baseColorCaptured) return;
        image.color = baseColor;
    }
}
