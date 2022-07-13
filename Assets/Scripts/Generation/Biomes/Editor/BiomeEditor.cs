using UnityEditor;

[CustomEditor(typeof(Biome))]
public class BiomeEditor : Editor
{
    private int selected = 0;
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        string[] equations = new[]
        {
            "Test 1",
            "Test 2",
            "Test 3",
            "Test 4",
            "Test 5"
        };

        selected = EditorGUILayout.Popup("Equation", selected, equations);
    }
}