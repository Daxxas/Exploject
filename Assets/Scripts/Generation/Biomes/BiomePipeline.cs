using System;
using System.Collections.Generic;
using Sirenix.Serialization;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "New Biome Pipeline", menuName = "Biomes/Pipeline", order = 0)]
public class BiomePipeline : ScriptableObject
{
    public int initialSize = 3;
    [SerializeField] public UnitySerializedDictionary initialBiomes;    
    public FastNoiseLite sourceInitialBiomes;
    public List<Stage> stages;
    
    public int GetExpandStageCount()
    {
        int count = 0;
        stages.ForEach(stage =>
        {
            if (stage is Expand) count++;
        });
        
        return count;
    }
    
    [ContextMenu("Force call OnEnable")]
    public void OnEnable()
    {
        // initialBiomes.UpdateDictionnary();
    }

    [ContextMenu("Update source noise")]
    public void UpdateSourceIntialNoise()
    {
        sourceInitialBiomes.CalculateFractalBounding();
    }
}

