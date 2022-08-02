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

    public NativeArray<BiomeHolder> GetBiomesForTerrainChunk(Vector2 coord)
    {
        NativeArray<BiomeHolder> returnBiomes = new NativeArray<BiomeHolder>(MapDataGenerator.Instance.supportedChunkSize * MapDataGenerator.Instance.supportedChunkSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        for (int x = 0; x < MapDataGenerator.Instance.supportedChunkSize; x++)
        {
            for (int z = 0; z < MapDataGenerator.Instance.supportedChunkSize; z++)
            {
                int biomeIdx = x + MapDataGenerator.Instance.supportedChunkSize * z;

                int2 pos = new int2((int)(x + coord.x * MapDataGenerator.Instance.supportedChunkSize), (int)(z + coord.y * MapDataGenerator.Instance.supportedChunkSize));
                
                returnBiomes[biomeIdx] = GetBiomeAtPos(pos).GetBiomeHolder();
            }
        }
        
        return returnBiomes;
    }

    public NativeList<BiomeHolder> GetBiomesInChunk(Vector2 coord)
    {
        NativeList<BiomeHolder> returnBiomes = new NativeList<BiomeHolder>(Allocator.Persistent);

        for (int x = 0; x < MapDataGenerator.Instance.supportedChunkSize; x++)
        {
            for (int z = 0; z < MapDataGenerator.Instance.supportedChunkSize; z++)
            {
                int2 pos = new int2((int)(x + coord.x * MapDataGenerator.Instance.supportedChunkSize), (int)(z + coord.y * MapDataGenerator.Instance.supportedChunkSize));

                BiomeHolder biomeAtPos = GetBiomeAtPos(pos).GetBiomeHolder();
                
                if (!returnBiomes.Contains(biomeAtPos))
                {
                    returnBiomes.Add(biomeAtPos);
                }
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