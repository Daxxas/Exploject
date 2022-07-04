using System;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class BiomeGenerator : MonoBehaviour
{
    [SerializeField] private BiomePipeline pipeline;
    [SerializeField] private Biome biome;
    public BiomePipeline Pipeline => pipeline;

    public ChunkBiome GetChunkBiome(Vector2 origin, int seed)
    {
        FastNoiseLite noise = new FastNoiseLite()
        {
            mSeed = seed,
            mNoiseType = FastNoiseLite.NoiseType.WhiteNoise,
            mFrequency = 1f
        };

        
        ChunkBiome chunkBiome = InitChunk(Vector2.zero, noise);

        foreach (var stage in pipeline.stages)
        {
            chunkBiome = stage.Apply((int) origin.x, (int) origin.y, chunkBiome, noise);
        }
        
        return chunkBiome;
    }
    
    private ChunkBiome InitChunk(Vector2 origin, FastNoiseLite noise)
    {
        ChunkBiome chunkBiome = new ChunkBiome()
        {
            origin = origin,
            width = pipeline.initialSize,
            data = new string[pipeline.initialSize, pipeline.initialSize]
        };
        pipeline.sourceInitialBiomes.SetSeed(noise.mSeed);

        for (int x = 0; x < chunkBiome.width; x++)
        {
            for (int z = 0; z < chunkBiome.width; z++)
            {
                chunkBiome[x, z] = pipeline.initialBiomes.GetRandomBiome(pipeline.sourceInitialBiomes,x,z).Id;
            }
        }

        return chunkBiome;
    }

    public ChunkBiome ApplyStage(ChunkBiome chunkBiome, FastNoiseLite noise, int index)
    {
       return pipeline.stages[index].Apply((int) chunkBiome.origin.x, (int) chunkBiome.origin.y, chunkBiome, noise);
    }

    public static void InitNewChunk(ChunkBiome chunkBiome, BiomePipeline pipeline, Vector2 origin)
    {
        chunkBiome.origin = origin;
        chunkBiome.width = pipeline.initialSize;
        chunkBiome.data = new string[pipeline.initialSize, pipeline.initialSize];
    }
}