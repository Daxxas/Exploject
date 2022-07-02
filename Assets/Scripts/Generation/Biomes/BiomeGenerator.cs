using System;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class BiomeGenerator : MonoBehaviour
{
    [SerializeField] private BiomePipeline pipeline;

    public BiomePipeline Pipeline => pipeline;
    public ChunkBiome GetChunkBiome(Vector2 origin)
    {
        return GetChunkBiome(origin, (uint)DateTime.Now.Millisecond);
    }

    public ChunkBiome GetChunkBiome(Vector2 origin, uint seed)
    {
        Random r = new Random();
        r.InitState(seed);
        
        ChunkBiome chunkBiome = InitChunk(Vector2.zero, ref r);

        foreach (var stage in pipeline.stages)
        {
            chunkBiome = stage.Apply((int) origin.x, (int) origin.y, chunkBiome, ref r);
        }
        
        return chunkBiome;
    }
    
    public ChunkBiome InitChunk(Vector2 origin, ref Random r)
    {
        ChunkBiome chunkBiome = new ChunkBiome()
        {
            origin = origin,
            width = pipeline.initialSize,
            data = new int[pipeline.initialSize, pipeline.initialSize]
        };

        
        for (int x = 0; x < chunkBiome.width; x++)
        {
            for (int z = 0; z < chunkBiome.width; z++)
            {
                chunkBiome.data[x, z] = r.NextInt(0, pipeline.initialBiomes.Count); 
            }
        }

        return chunkBiome;
    }

    public ChunkBiome ApplyStage(ChunkBiome chunkBiome, ref Random r, int index)
    {
       return pipeline.stages[index].Apply((int) chunkBiome.origin.x, (int) chunkBiome.origin.y, chunkBiome, ref r);
    }

    public static void InitNewChunk(ChunkBiome chunkBiome, BiomePipeline pipeline, Vector2 origin)
    {
        chunkBiome.origin = origin;
        chunkBiome.width = pipeline.initialSize;
        chunkBiome.data = new int[pipeline.initialSize, pipeline.initialSize];
    }
}