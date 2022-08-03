using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class BiomeGenerator : MonoBehaviour
{
    private static BiomeGenerator instance;
    public static BiomeGenerator Instance => instance;

    [SerializeField] private BiomePipeline pipeline;
    public BiomePipeline Pipeline => pipeline;

    private Dictionary<FixedString32Bytes, Biome> biomeDictionary = new Dictionary<FixedString32Bytes, Biome>();

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);

        foreach (var biome in pipeline.biomes)
        {
            biomeDictionary.Add(biome.GetBiomeHolder().id, biome);
        }
    }

    private BiomeHolder[] GetBiomesForChunk(Vector2 coord, int resolution)
    {
        BiomeHolder[] returnBiomes = new BiomeHolder[MapDataGenerator.Instance.supportedChunkSize * MapDataGenerator.Instance.supportedChunkSize];

        for (int x = 0; x < MapDataGenerator.Instance.supportedChunkSize; x++)
        {
            for (int z = 0; z < MapDataGenerator.Instance.supportedChunkSize; z++)
            {
                int biomeIdx = x + MapDataGenerator.Instance.supportedChunkSize * z;
                
                int2 pos = new int2((int)((x - (resolution * 3 * coord.x)) + (coord.x * MapDataGenerator.Instance.supportedChunkSize) - resolution), 
                                    (int)((z - (resolution * 3 * coord.y)) + (coord.y * MapDataGenerator.Instance.supportedChunkSize) - resolution));

                returnBiomes[biomeIdx] = GetBiomeAtPos(pos).GetBiomeHolder();
            }
        } 

        return returnBiomes;
    }
    
    public NativeArray<BiomeHolder> GetBiomesOfTerrainChunkForJob(Vector2 coord, int resolution)
    {
        var biomeArray = GetBiomesForChunk(coord, resolution);
        
        NativeArray<BiomeHolder> returnArray = new NativeArray<BiomeHolder>(biomeArray.Length, Allocator.TempJob);
        returnArray.CopyFrom(biomeArray);
        
        return returnArray;
    }

    public NativeList<BiomeHolder> GetBiomesInChunk(Vector2 coord, int resolution)
    {
        NativeList<BiomeHolder> returnBiomes = new NativeList<BiomeHolder>(Allocator.Persistent);
        var biomeArray = GetBiomesForChunk(coord, resolution);
        for (int i = 0; i < biomeArray.Length; i++)
        {
            if (!returnBiomes.Contains(biomeArray[i]))
            {
                returnBiomes.Add(biomeArray[i]);
            }
        }
        
        return returnBiomes;
    }

    public Biome GetBiomeFromId(FixedString32Bytes id)
    {
        return biomeDictionary[id];
    }
    
    public Biome GetBiomeAtPos(int2 pos)
    {
        float yContinentalness = pipeline.GenerationConfiguration.yContinentalness.GetNoise(GenerationInfo.seed, pos.x, pos.y);
        
        // Debug.Log($"[BIOME] Sampling {pos.x}, {pos.y} = {yContinentalness}");

        if (yContinentalness > pipeline.landBiomeThreshold)
        {
            // return land biome
            return pipeline.biomes[1];
        }
        else
        {
            // return water biome
            return pipeline.biomes[0];
        }
    }
}