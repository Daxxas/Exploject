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
    [SerializeField] private Biome biome;
    public BiomePipeline Pipeline => pipeline;

    private Dictionary<int2, ChunkBiome> biomeChunkDic = new Dictionary<int2, ChunkBiome>();

    
    private int chunkBiomeSize;
    
    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this);

        chunkBiomeSize = pipeline.GetChunkBiomeSize();
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

    public ChunkBiome GetChunkBiome(int2 origin, int seed)
    {
        // TODO : Threadify this

        if (biomeChunkDic.ContainsKey(origin))
        {
            return biomeChunkDic[origin];
        }
        else
        {
            ChunkBiome chunkBiome = InitChunk(origin * (pipeline.initialSize + 3), seed);

            foreach (var stage in pipeline.stages)
            {
                chunkBiome = stage.Apply(chunkBiome, seed);
            }

            biomeChunkDic[origin] = chunkBiome;
            
            return biomeChunkDic[origin];
        }
    }

    public void ResetBiomeDictionnary()
    {
        biomeChunkDic.Clear();
    }
    
    public Biome GetBiomeAtPos(int2 pos)
    {
        int2 adaptedPos = pos;
        
        if (adaptedPos.x < 0)
        {
            adaptedPos.x += -chunkBiomeSize;
        }
        if (adaptedPos.y < 0)
        {
            adaptedPos.y += -chunkBiomeSize;
        }
        
        int2 correspondingBiomeChunk = new int2(adaptedPos.x / chunkBiomeSize, adaptedPos.y / chunkBiomeSize);

        ChunkBiome chunkBiome = GetChunkBiome(correspondingBiomeChunk, GenerationInfo.Seed);

        // Debug.Log($"input : {pos} | getting position for chunk {correspondingBiomeChunk} at {pos.x % chunkBiomeSize} {pos.y % chunkBiomeSize}");
        
        Biome biome = chunkBiome[math.abs(pos.x) % chunkBiomeSize, math.abs(pos.y) % chunkBiomeSize];
        
        return biome;
    }
    
    private ChunkBiome InitChunk(int2 origin, int seed)
    {
        ChunkBiome chunkBiome = new ChunkBiome(pipeline.initialSize, origin);

        for (int x = 0; x < chunkBiome.width; x++)
        {
            for (int z = 0; z < chunkBiome.width; z++)
            {
                chunkBiome[x, z] = pipeline.initialBiomes.GetRandomBiome(pipeline.sourceInitialBiomes, seed, x+origin.x, z+origin.y);
            }
        }

        return chunkBiome;
    }

    public static void InitNewChunk(ChunkBiome chunkBiome, BiomePipeline pipeline, int2 origin)
    {
        chunkBiome.origin = origin;
        chunkBiome.width = pipeline.initialSize;
        chunkBiome.data = new Biome[pipeline.initialSize, pipeline.initialSize];
    }
}