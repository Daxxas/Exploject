
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

[Serializable]
public class WeightedBiomeList
{
    [SerializeField]
    private List<WeightedBiome> biomesList = new List<WeightedBiome>();
    [SerializeField]
    public FastNoiseLite sourceInitialBiomes;

    private int TotalWeight()
    {
        int total = 0;
        foreach (var biome in biomesList)
        {
            total += biome.weight;
        }
        return total;
    }
    
    public Biome GetRandomBiome(int x, int z)
    {
        int randomNumber = MathUtil.NormalizeIndex(sourceInitialBiomes.GetNoise(x, z), TotalWeight());
        
        WeightedBiome selectedBiome = this[0];

        foreach (var weightedBiome in biomesList)
        {
            if (randomNumber < weightedBiome.weight)
            {
                selectedBiome = weightedBiome;
                break;
            }

            randomNumber -= weightedBiome.weight;
        }

        return selectedBiome.biome;
    }

    
    public WeightedBiome this[int index]
    {
        get => biomesList[index];
        set => biomesList[index] = value;
    }
}

[Serializable]
public struct WeightedBiome
{
    public Biome biome;
    public int weight;
}