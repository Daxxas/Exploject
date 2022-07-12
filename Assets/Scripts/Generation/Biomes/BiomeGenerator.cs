using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class BiomeGenerator : MonoBehaviour
{
    [SerializeField] private BiomePipeline pipeline;
    [SerializeField] private Biome biome;
    public BiomePipeline Pipeline => pipeline;

    readonly Test structTest = new Test();
    [ContextMenu("test")]
    public void test()
    {
        
        var value = PlainNoise.GetNoise(1334, 5, 14);
        Debug.Log(value);
        // var value = structTest.ValueTest(5,10);
    }

    private struct Test
    {
        public readonly int ValueTest(int a, int b)
        {
            return a + b;
        }
    }
    
    public ChunkBiome GetChunkBiome(int2 origin, int seed)
    {
        // TODO : Threadify this

        ChunkBiome chunkBiome = InitChunk(origin, seed);

        foreach (var stage in pipeline.stages)
        {
            chunkBiome = stage.Apply(chunkBiome, seed);
        }

        return chunkBiome;
    }
    
    private ChunkBiome InitChunk(int2 origin, int seed)
    {
        ChunkBiome chunkBiome = new ChunkBiome(pipeline.initialSize, origin);

        for (int x = 0; x < chunkBiome.width; x++)
        {
            for (int z = 0; z < chunkBiome.width; z++)
            {
                chunkBiome[x, z] = pipeline.initialBiomes.GetRandomBiome(pipeline.sourceInitialBiomes, x+origin.x, z+origin.y, seed);
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