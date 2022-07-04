using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[CreateAssetMenu(fileName = "New Expand Stage", menuName = "Biomes/Expand Stage", order = 3)]
public class Expand : Stage
{
    public override ChunkBiome Apply(int originX, int originZ, ChunkBiome oldChunk, FastNoiseLite noise)
    {
        int newWidth = oldChunk.width * 2 - 1;
        ChunkBiome newChunk = new ChunkBiome()
        {
            width = newWidth,
            data = new string[newWidth,newWidth],
            origin = new Vector2(originX, originZ)
        };
        
        for (int x = 0; x < oldChunk.width; x++)
        {
            for (int z = 0; z < oldChunk.width; z++)
            {
                int newChunkX = x * 2;
                int newChunkZ = z * 2;
        
                newChunk.data[newChunkX, newChunkZ] = oldChunk[x, z];
        
                if(z != oldChunk.width - 1)
                {
                    newChunk.data[newChunkX, newChunkZ + 1] = GetMiddle(x, z + 1, noise,oldChunk[x, z], oldChunk[x, z + 1]);
                }
        
                if (x != oldChunk.width - 1)
                {
                    newChunk.data[newChunkX + 1, newChunkZ] = GetMiddle(x+1,z , noise,oldChunk[x, z], oldChunk[x + 1, z]);
                }
        
                if (x != oldChunk.width - 1 && z != oldChunk.width - 1)
                {
                    newChunk.data[newChunkX + 1, newChunkZ + 1] = GetMiddle(x+1,z+1, noise, oldChunk[x, z], oldChunk[x + 1, z], oldChunk[ x + 1, z + 1], oldChunk[x, z + 1]);
                }
                
            }
        }
        
        return newChunk;
    }

    private string GetMiddle(int x, int z, FastNoiseLite noise, params string[] others)
    {
        return others[MathUtil.NormalizeIndex(noise.GetNoise(x,z), others.Length)];
    }
}
