using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile(CompileSynchronously = true)]
public struct FindVertexColorJob : IJob
{
    // Inputs
    public NativeParallelHashMap<Edge, Vertex> uniqueVertices;
    public int2 chunkPosition;
    public float transitionRadius;
    public float threshold;
    public float radiusFactor;
    public NativeList<BiomeHolder> biomes;
    public int seed;

    [ReadOnly]
    public SampledGradient biomeGradient;
    
    // Noises
    public FastNoiseLite yContinentalness;
    
    public void Execute()
    {
        foreach (var uniqueVertex in uniqueVertices)
        {
            float worldPosX = uniqueVertex.Value.position.x + (chunkPosition.x * MapDataGenerator.ChunkSize);
            float worldPosZ = uniqueVertex.Value.position.z + (chunkPosition.y * MapDataGenerator.ChunkSize);

            float4 averageColor = GetBiomeAtPos(new float2(worldPosX, worldPosZ)).color;
            
            // for (float x = -transitionRadius * radiusFactor; x < transitionRadius * radiusFactor; x+= radiusFactor)
            // {
            //     for (float z = -transitionRadius * radiusFactor; z < transitionRadius * radiusFactor; z+= radiusFactor)
            //     {
            //         float weight = math.distance(new float2(x, z), new float2(0, 0)) / (transitionRadius * radiusFactor);
            //         // Debug.Log("weight " + weight);
            //         averageColor = (averageColor + GetBiomeAtPos(new float2(worldPosX + x, worldPosZ + z)).color) /2 ;
            //     }
            // }

            uniqueVertex.Value.color = averageColor;
        }
    }
    
    
    private BiomeHolder GetBiomeAtPos(float2 pos)
    {
        float yCont = yContinentalness.GetNoise(seed, pos.x, pos.y);
        
        BiomeHolder returnBiomeHolder;
        
        if (yCont > threshold)
        {
            // return land biome
            returnBiomeHolder =  biomes[1];
        }
        else
        {
            // return water biome
            returnBiomeHolder =  biomes[0];
        }

        returnBiomeHolder.color = biomeGradient.EvaluateLerp(yCont);

        return returnBiomeHolder;
    }
}