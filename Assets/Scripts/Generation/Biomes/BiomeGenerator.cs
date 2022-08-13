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

    private SampledGradient biomeGradient;
    public SampledGradient BiomeGradient => biomeGradient;

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);

        foreach (var biome in pipeline.biomes)
        {
            biomeDictionary.Add(biome.GetBiomeHolder().id, biome);
        }

        biomeGradient = new SampledGradient(pipeline.biomeGradient, 1024);
    }

    private void OnDestroy()
    {
        biomeGradient.Dispose();
    }

    public BiomeHolder[] GetBiomesForChunk(Vector2 coord, int resolution)
    {
        int roundedTransitionRadius = (int)(pipeline.transitionRadius + 0.5f);
        
        BiomeHolder[] calculatedBiomes = new BiomeHolder[(MapDataGenerator.SupportedChunkSize + roundedTransitionRadius * 2) *
                                                         (MapDataGenerator.SupportedChunkSize + roundedTransitionRadius * 2)];
        BiomeHolder[] returnBiomes = new BiomeHolder[MapDataGenerator.SupportedChunkSize * MapDataGenerator.SupportedChunkSize];

        for (int x = 0; x < MapDataGenerator.SupportedChunkSize + pipeline.transitionRadius * 2; x++)
        {
            for (int z = 0; z < MapDataGenerator.SupportedChunkSize + pipeline.transitionRadius * 2; z++)
            {
                int biomeIdx = x + (MapDataGenerator.SupportedChunkSize + roundedTransitionRadius * 2) * z;
                
                int2 pos = new int2((int)((x - (resolution * 3 * coord.x)) + (coord.x * (MapDataGenerator.SupportedChunkSize)) - resolution), 
                                    (int)((z - (resolution * 3 * coord.y)) + (coord.y * (MapDataGenerator.SupportedChunkSize)) - resolution));

                calculatedBiomes[biomeIdx] = GetBiomeAtPos(pos).GetBiomeHolder();
            }
        }
        
        for (int x = 0; x < MapDataGenerator.SupportedChunkSize; x++)
        {
            for (int z = 0; z < MapDataGenerator.SupportedChunkSize; z++)
            {
                int correctX = x + roundedTransitionRadius;
                int correctZ = z + roundedTransitionRadius;
                
                int firstIndex = correctX + (MapDataGenerator.SupportedChunkSize + roundedTransitionRadius * 2) * correctZ;
                float4 averageColor = calculatedBiomes[firstIndex].color;
                
                for (int transitionOffsetX = -roundedTransitionRadius; transitionOffsetX < pipeline.transitionRadius; transitionOffsetX++)
                {
                    for (int transitionOffsetZ = -roundedTransitionRadius; transitionOffsetZ < pipeline.transitionRadius; transitionOffsetZ++)
                    {
                        int sampledIndex = correctX + transitionOffsetX + (MapDataGenerator.SupportedChunkSize + roundedTransitionRadius * 2) * (correctZ + transitionOffsetZ);
                        averageColor = (averageColor + calculatedBiomes[sampledIndex].color) / 2;
                    }
                }
                
                int returnBiomeIdx = x + MapDataGenerator.SupportedChunkSize * z;
                int calculatedBiomeIdx = correctX + (MapDataGenerator.SupportedChunkSize + roundedTransitionRadius * 2) * correctZ;
                
                returnBiomes[returnBiomeIdx] = calculatedBiomes[calculatedBiomeIdx];
                returnBiomes[returnBiomeIdx].color = averageColor;

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