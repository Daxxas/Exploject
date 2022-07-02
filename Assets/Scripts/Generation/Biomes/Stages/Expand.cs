using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[CreateAssetMenu(fileName = "New Expand Stage", menuName = "Biomes/Expand Stage", order = 3)]
public class Expand : Stage
{
    public override ChunkBiome Apply(int originX, int originZ, ChunkBiome oldChunk, ref Random r)
    {
        int newWidth = oldChunk.width * 2 - 1;
        ChunkBiome newChunk = new ChunkBiome()
        {
            width = newWidth,
            data = new int[newWidth,newWidth],
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
                    newChunk.data[newChunkX, newChunkZ + 1] = GetMiddle(oldChunk[x, z], oldChunk[x, z + 1], ref r);
                }

                if (x != oldChunk.width - 1)
                {
                    newChunk.data[newChunkX + 1, newChunkZ] = GetMiddle(oldChunk[x, z], oldChunk[x + 1, z], ref r);
                }

                if (x != oldChunk.width - 1 && z != oldChunk.width - 1)
                {
                    newChunk.data[newChunkX + 1, newChunkZ + 1] = GetMiddle(oldChunk[x, z], oldChunk[x + 1, z], oldChunk[ x + 1, z + 1], oldChunk[x, z + 1], r);
                }
                
            }
        }
        
        return newChunk;
    }

    private int GetMiddle(int a, int b, ref Random r)
    {
        return r.NextBool() ? a : b;
    }

    private int GetMiddle(int a, int b, int c, int d, Random r)
    {
        switch (r.NextInt(0,4))
        {
            case 0:
                return a;
            case 1:
                return b;
            case 2:
                return c;
            default:
                return d;
        }
    }
}
