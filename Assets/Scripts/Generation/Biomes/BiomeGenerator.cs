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
    [SerializeField] private Biome oceanBiome;
    [SerializeField] private Biome plainBiome;
    public BiomePipeline Pipeline => pipeline;

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);
    }

    public NativeArray<FunctionPointer<TerrainEquation.TerrainEquationDelegate>> GetBiomesForTerrainChunk(Vector2 coord)
    {
        NativeArray<FunctionPointer<TerrainEquation.TerrainEquationDelegate>> returnBiomes = new NativeArray<FunctionPointer<TerrainEquation.TerrainEquationDelegate>>(MapDataGenerator.Instance.supportedChunkSize * MapDataGenerator.Instance.supportedChunkSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

        for (int x = 0; x < MapDataGenerator.Instance.supportedChunkSize; x++)
        {
            for (int z = 0; z < MapDataGenerator.Instance.supportedChunkSize; z++)
            {
                int biomeIdx = (int) (MapDataGenerator.Instance.supportedChunkSize * x + z);
                
                returnBiomes[biomeIdx] = GetBiomeAtPos(new int2((int)(x + coord.x * MapDataGenerator.Instance.supportedChunkSize), (int)(z + coord.y * MapDataGenerator.Instance.supportedChunkSize))).TerrainEquation();
            }            
        }
        
        return returnBiomes;
    }
    
    public Biome GetBiomeAtPos(int2 pos)
    {
        // int continentalnessIndex = MathUtil.NormalizeIndex(pipeline.GenerationConfiguration.yContinentalness.GetNoise(GenerationInfo.seed, pos.x, pos.y), 2);
        //
        // if (continentalnessIndex == 0)
        // {
        //     return oceanBiome;
        // }
        // else
        // {
        //     return plainBiome;
        // }
        
        return plainBiome;
    }
}