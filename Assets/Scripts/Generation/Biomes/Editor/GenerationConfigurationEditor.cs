using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GenerationConfiguration))]
public class GenerationConfigurationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        GenerationConfiguration config = (GenerationConfiguration)target;
        
        EditorGUILayout.CurveField("Squash Continent Curve", config.squashContinentCurve, Color.cyan, new Rect(-1, 0, 2f, MapDataGenerator.chunkHeight));
        EditorGUILayout.CurveField("y Continent Curve", config.yContinentCurve, Color.green, new Rect(-1, 0, 2f, MapDataGenerator.chunkHeight));
    }


}
