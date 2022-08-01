using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(FastNoiseLite))]
public class FastNoiseLiteEditor : PropertyDrawer
{
    private float previewSize = 300f;
    private int previewResolution = 128;
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.PropertyField(position, property, label, true);

        FastNoiseLite noise = (FastNoiseLite) fieldInfo.GetValue(property.serializedObject.targetObject);
        
        if (property.isExpanded)
        {
            Texture2D noisePreview = new Texture2D(previewResolution, previewResolution);
            noisePreview.filterMode = FilterMode.Point;

            for (int x = 0; x < previewResolution; x++)
            {
                for (int z = 0; z < previewResolution; z++)
                {
                    float val = noise.GetNoise(1, x, z);
                    float transformedVal = (val + 1) / 2;
            
                    Color color = new Color(transformedVal, transformedVal, transformedVal);
                    
                    noisePreview.SetPixel(x,z,color);
                }            
            }
            
            noisePreview.Apply();
            GUIStyle style = new GUIStyle();
            style.normal.background = noisePreview;
            EditorGUI.LabelField(new Rect(((position.xMax + position.xMin - previewSize) / 2), position.yMax - previewSize, previewSize, previewSize), GUIContent.none, style);
        }
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.isExpanded)
            return EditorGUI.GetPropertyHeight(property) + previewSize + 10f;
        return EditorGUI.GetPropertyHeight(property);
    }
}