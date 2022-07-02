using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BiomeVisualizer))]
public class BiomeVisualizerEditor : Editor
{
    private int stepCount = 0;

    private ChunkBiome currentChunkBiome;
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        BiomeVisualizer biomeVisualizer = (BiomeVisualizer) target;

        if (GUILayout.Button("Generate pipeline (random seed)"))
        {
            biomeVisualizer.GenerateRandomTexture();
        }
        if (GUILayout.Button("Generate pipeline (with seed)"))
        {
            biomeVisualizer.GenerateRandomTexture(false);
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Next step"))
        {
            if (stepCount == 0)
            {
                currentChunkBiome = biomeVisualizer.ApplyStep(new ChunkBiome(), stepCount);
            }
            else
            {
                currentChunkBiome = biomeVisualizer.ApplyStep(currentChunkBiome, stepCount);
            }
            stepCount++;
        }
        if (GUILayout.Button("Reset"))
        {
            stepCount = 0;
            biomeVisualizer.ResetVisualization();
        }

       
    }
}