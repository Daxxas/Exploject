using UnityEngine;
using Random = Unity.Mathematics.Random;

[CreateAssetMenu(fileName = "New Smooth Stage", menuName = "Biomes/Smooth Stage", order = 4)]
public class Smooth : Stage
{
    public override ChunkBiome Apply(ChunkBiome chunkBiome, int seed)
    {
        for (int x = 0; x < chunkBiome.width; x++)
        {
            for (int z = 0; z < chunkBiome.width; z++)
            {
                chunkBiome.data[x, z] = SmoothPosition(ref chunkBiome, x, z, seed);
            }
        }

        return chunkBiome;
    }

    private Biome SmoothPosition(ref ChunkBiome chunkBiome, int x, int z, int seed)
    {
        if (x == 0 || z == 0  || x == chunkBiome.width - 1 || z == chunkBiome.width - 1)
        {
            return chunkBiome[x, z];
        }
        
        Biome top = chunkBiome[x + 1, z];
        Biome bottom = chunkBiome[x - 1, z];
        Biome left = chunkBiome[x, z+1];
        Biome right = chunkBiome[x, z-1];
        
        bool vert = (top ==  bottom);
        bool horiz = (left == right);
              
        if(vert && horiz) {
            return MathUtil.NormalizeIndex(noise.GetNoise(x, z, seed), 2) == 0 ? left : top;
        }
        
        if(vert) return top;
        if(horiz) return left;

        return chunkBiome[x, z];
    }
}
