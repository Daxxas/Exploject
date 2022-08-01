using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New Border Stage", menuName = "Biomes/Border Stage", order = 5)]
public class Border : Stage
{
    public string from;
    public string replaceTag;
    public WeightedBiomeList replacingBiomes;

    public override ChunkBiome Apply(ChunkBiome chunkBiome, int seed)
    {
        for (int x = 0; x < chunkBiome.width; x++)
        {
            for (int z = 0; z < chunkBiome.width; z++)
            {
                if (chunkBiome[x, z].tags.Contains(from))
                {
                    for (int xi = -1; xi <= 1; xi++)
                    {
                        for (int zi = -1; zi <= 1; zi++)
                        {
                            if ((xi == 0 && zi == 0) ||
                                (x + xi) >= chunkBiome.width ||
                                (z + zi) >= chunkBiome.width ||
                                (x + xi) < 0 ||
                                (z + zi) < 0)
                            {
                                continue;
                            }
                            
                            if (chunkBiome[x + xi, z + zi].tags.Contains(replaceTag))
                            {
                                Biome newBiome = replacingBiomes.GetRandomBiome(noise, seed, x, z);
                                if (!newBiome.isSelf)
                                {
                                    chunkBiome[x + xi, z + zi] = newBiome;
                                }
                            } 
                        }                        
                    }
                }
            }
        }

        return chunkBiome;
    }
}