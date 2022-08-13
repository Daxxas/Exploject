using System;
using System.Reflection;
using Unity.Mathematics;
using Unity.Properties;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEditor.UIElements;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;


public class FastNoiseLiteWindow : EditorWindow
{
    private static float previewSize = 300f;
    private static float windowHeight = 500f;
    private int previewResolution = 128;
    [SerializeField] private FastNoiseLite noise;
    SerializedProperty noiseProperty;

    [MenuItem("FastNoiseLite/FastNoiseLite Preview")]
    public static void ShowWindow()
    {
        // Get existing open window or if none, make a new one:
        FastNoiseLiteWindow window = (FastNoiseLiteWindow)EditorWindow.GetWindow(typeof(FastNoiseLiteWindow));
        window.maxSize = new Vector2(previewSize, windowHeight);
        window.minSize = window.maxSize;

        GetWindow<FastNoiseLiteWindow>(false, "FastNoiseLite Preview", true);
        
    }
    
    void OnGUI()
    {
        
        
        if (Selection.activeObject == null)
        {
            return;
        }
        
        
        noiseProperty = new SerializedObject(Selection.activeObject).GetIterator();

        while (noiseProperty.Next(true))
        {
            if (noiseProperty.isExpanded && EditorHelper.GetTargetObjectOfProperty(noiseProperty) is FastNoiseLite)
            {
                break;
            }
        }
        

        noise = (FastNoiseLite) EditorHelper.GetTargetObjectOfProperty(noiseProperty);
        
        Texture2D noisePreview = new Texture2D(previewResolution, previewResolution);
        noisePreview.filterMode = FilterMode.Point;

        float maxValue = -1;
        float minValue = 1;
        float mean = 0;

        
        for (int x = 0; x < noisePreview.width; x++)
        {
            for (int z = 0; z < noisePreview.height; z++)
            {
                float val = noise.GetNoise(1, x, z);

                if (val > maxValue)
                {
                    maxValue = val;
                }

                if (val < minValue)
                {
                    minValue = val;
                }
                
                
                mean += val;
                
                float transformedVal = (val + 1) / 2;
                Color color = new Color(transformedVal, transformedVal, transformedVal);

                noisePreview.SetPixel(x, z, color);
            }
        }
        mean /= noisePreview.width * noisePreview.height;

        noisePreview.Apply();
        GUIStyle style = new GUIStyle();
        style.normal.background = noisePreview;

        EditorGUI.LabelField(new Rect((position.width - previewSize) / 2, (position.height - previewSize), previewSize, previewSize), GUIContent.none, style);
        EditorGUILayout.FloatField("Max value", maxValue);
        EditorGUILayout.FloatField("Min value", minValue);
        EditorGUILayout.FloatField("Mean", mean);
        testThreshold = EditorGUILayout.Slider("Threshold", testThreshold, -1f, 1f);
    }
    
    float testThreshold = 0f;
    
    void OnInspectorUpdate()
    {
        Repaint();
    }
}