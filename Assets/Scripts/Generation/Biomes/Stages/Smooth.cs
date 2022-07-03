using UnityEngine;
using Random = Unity.Mathematics.Random;

[CreateAssetMenu(fileName = "New Smooth Stage", menuName = "Biomes/Smooth Stage", order = 4)]
public class Smooth : Stage
{
    public override ChunkBiome Apply(int originX, int originZ, ChunkBiome chunkBiome, ref Random r)
    {
        for (int x = 0; x < chunkBiome.width; x++)
        {
            for (int z = 0; z < chunkBiome.width; z++)
            {
                chunkBiome.data[x,z] = SmoothPosition(ref chunkBiome, x, z);
            }
        }

        return chunkBiome;
    }

    private int SmoothPosition(ref ChunkBiome chunkBiome, int x, int z)
    {
        if (x == 0 || z == 0  || x == chunkBiome.width - 1 || z == chunkBiome.width - 1)
        {
            return chunkBiome[x, z];
        }
        
        int top = chunkBiome[x + 1, z];
        int bottom = chunkBiome[x - 1, z];
        int left = chunkBiome[x, z+1];
        int right = chunkBiome[x, z-1];
        
        bool vert = (top ==  bottom);
        bool horiz = (left == right);
                
        if(vert) return top;
        if(horiz) return left;

        return chunkBiome[x, z];
    }
}
