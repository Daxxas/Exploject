using System;
using System.Collections.Generic;
using Sirenix.Serialization;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "New Biome Pipeline", menuName = "Biomes/Pipeline", order = 0)]
public class BiomePipeline : ScriptableObject
{
    public int initialSize = 3;
    [SerializeField] public WeightedBiomeList initialBiomes;
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
    
    public void OnValidate()
    {
        initialBiomes.sourceInitialBiomes.CalculateFractalBounding();
    }
}

