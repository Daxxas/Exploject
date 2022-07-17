using UnityEditor;
using UnityEngine;
using UnityEngine.SocialPlatforms;

[CustomEditor(typeof(MapDataGenerator))]
public class MapDataGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        MapDataGenerator mapDataGenerator = (MapDataGenerator)target;
         
        EditorGUILayout.CurveField("Continent Curve", mapDataGenerator.continentCurve, Color.cyan, new Rect(-1, 0, 2f, MapDataGenerator.chunkHeight));
    }
}