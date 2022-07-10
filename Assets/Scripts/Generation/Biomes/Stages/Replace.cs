using System.Collections.Generic;
using UnityEngine;
using Random = Unity.Mathematics.Random;


[CreateAssetMenu(fileName = "New Replace List Stage", menuName = "Biomes/Replace List Stage", order = 4)]
public class Replace : Stage
{
    public List<string> replacedTags;
    public WeightedBiomeList replacingBiomes;

    public override ChunkBiome Apply(ChunkBiome chunkBiome)
    {
        for (int x = 0; x < chunkBiome.width; x++)
        {
            for (int z = 0; z < chunkBiome.width; z++)
            {
                foreach (string tag in replacedTags)
                {
                    if (chunkBiome[x, z].tags.Contains(tag))
                    {   
                        Biome newBiome = replacingBiomes.GetRandomBiome(noise, x, z);
                        if (!newBiome.isSelf)
                        {
                            chunkBiome[x, z] = newBiome;
                        }
                        break;
                    }
                }
            }
        }

        return chunkBiome;
    }
}