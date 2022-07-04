using System;
using System.Collections.Generic;
using Sirenix.Serialization;
using UnityEngine;

[CreateAssetMenu(fileName = "New Biome Pipeline", menuName = "Biomes/Pipeline", order = 0)]
public class BiomePipeline : ScriptableObject
{
    public int initialSize = 3;
    [SerializeField] public WeightedBiomeList initialBiomes;
    public FastNoiseLite sourceInitialBiomes;
    public List<Stage> stages;

    [ContextMenu("Force call OnEnable")]
    public void OnEnable()
    {
        initialBiomes.UpdateDictionnary();
    }

    [ContextMenu("Update source noise")]
    public void UpdateSourceIntialNoise()
    {
        sourceInitialBiomes.CalculateFractalBounding();
    }
}

