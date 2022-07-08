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
        
        ChunkBiome chunkBiome = InitChunk(origin, seed);

        foreach (var stage in pipeline.stages)
        {
            stage.noise.mSeed = seed;
            chunkBiome = stage.Apply(chunkBiome);
        }
        
        return chunkBiome;
    }
    
    private ChunkBiome InitChunk(Vector2 origin, int seed)
    {
        FastNoiseLite noise = new FastNoiseLite()
        {
            mSeed = seed,
            mNoiseType = FastNoiseLite.NoiseType.WhiteNoise,
            mFrequency = 10f
        };
        
        ChunkBiome chunkBiome = new ChunkBiome(pipeline.initialSize, origin);
        pipeline.sourceInitialBiomes.SetSeed(noise.mSeed);

        for (int x = 0; x < chunkBiome.width; x++)
        {
            for (int z = 0; z < chunkBiome.width; z++)
            {
                // chunkBiome[x, z] = pipeline.initialBiomes.GetRandomBiome(pipeline.sourceInitialBiomes,x+(int)origin.x,z+(int)origin.y).Id;
            }
        }

        return chunkBiome;
    }

    public ChunkBiome ApplyStage(ChunkBiome chunkBiome, int index)
    {
       return pipeline.stages[index].Apply(chunkBiome);
    }

    public static void InitNewChunk(ChunkBiome chunkBiome, BiomePipeline pipeline, Vector2 origin)
    {
        chunkBiome.origin = origin;
        chunkBiome.width = pipeline.initialSize;
        chunkBiome.data = new string[pipeline.initialSize, pipeline.initialSize];
    }
}