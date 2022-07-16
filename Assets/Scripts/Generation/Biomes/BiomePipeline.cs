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
    [SerializeField] public FastNoiseLite sourceInitialBiomes;
    public List<Stage> stages;
    public int GetChunkBiomeSize()
    {
        int size = initialSize + 4;
        foreach (var stage in stages)
        {
            if (stage is Expand) size = size * 2 - 1;
        }

        return size;
    }
    
    public void OnValidate()
    {
        // sourceInitialBiomes.mFractalBounding = FastNoiseLite.CalculateFractalBounding(sourceInitialBiomes.mGain, sourceInitialBiomes.mOctaves);
    }
}

