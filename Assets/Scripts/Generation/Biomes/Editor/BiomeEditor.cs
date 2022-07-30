using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Biome))]
public class BiomeEditor : Editor
{
    private int selected = 0;
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        
        // List<string> typeNames = new List<string>();
        // List<Type> finalTypes = new List<Type>();
        //
        // Assembly csharp = Assembly.Load("Assembly-CSharp");
        //
        // Type[] types = csharp.GetTypes();
        //
        // foreach (var type in types)
        // {
        //     if (type.IsSubclassOf(typeof(TerrainEquation)))
        //     {
        //         typeNames.Add(type.Name);
        //         finalTypes.Add(type);
        //     }
        // }
        //
        // Biome biome = (Biome) target;
        //
        // // Initialize the correct index
        // selected = finalTypes.FindIndex(type =>
        // {
        //     if (type.Name == biome.typeEquation)
        //     {
        //         return true;
        //     }
        //
        //     return false;
        // });
        //
        // // If nothing is selected, prevent error
        // if(selected == -1)
        // {
        //     selected = 0;
        // }
        //
        // selected = EditorGUILayout.Popup("Equation", selected, typeNames.ToArray());
        //
        // // biome.typeEquation = finalTypes[selected].Name;
        // // biome.methodEquation = finalTypes[selected].GetMethod("GetResult", BindingFlags.Public | BindingFlags.Static)?.Name;
        // EditorUtility.SetDirty(target);

    }
}