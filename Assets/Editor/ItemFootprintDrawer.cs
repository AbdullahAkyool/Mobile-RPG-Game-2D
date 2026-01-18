using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ItemFootprint))]
public class ItemFootprintDrawer : PropertyDrawer
{
    private const int CellPx = 22;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var heightProp = property.FindPropertyRelative("height");
        int h = Mathf.Max(1, heightProp.intValue);
        return (h * CellPx) + 80; // üst alanlar + grid
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var widthProp = property.FindPropertyRelative("width");
        var heightProp = property.FindPropertyRelative("height");
        var filledProp = property.FindPropertyRelative("filled");

        position.height = EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(position, widthProp);
        position.y += position.height + 2;
        EditorGUI.PropertyField(position, heightProp);
        position.y += position.height + 6;

        int w = Mathf.Max(1, widthProp.intValue);
        int h = Mathf.Max(1, heightProp.intValue);
        int expected = w * h;

        if (filledProp.arraySize != expected)
            filledProp.arraySize = expected;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int idx = y * w + x;
                var cell = filledProp.GetArrayElementAtIndex(idx);

                var rect = new Rect(
                    position.x + x * CellPx,
                    position.y + y * CellPx,
                    CellPx,
                    CellPx
                );

                GUI.backgroundColor = cell.boolValue ? Color.green : Color.white;
                if (GUI.Button(rect, cell.boolValue ? "■" : " "))
                    cell.boolValue = !cell.boolValue;
            }
        }

        GUI.backgroundColor = Color.white;
        EditorGUI.EndProperty();
    }
}
