using UnityEngine;

[CreateAssetMenu(fileName = "New Expand Stage", menuName = "Biomes/Expand Stage", order = 3)]
public class Expand : Stage
{
    public override ChunkBiome Apply(ChunkBiome chunkBiome, int seed)
    {
        return chunkBiome.Expand(this, seed);
    }

    public Biome GetMiddle(int x, int z, int seed, params Biome[] others)
    {
        return others[MathUtil.NormalizeIndex(noise.GetNoise(x,z, seed), others.Length)];
    }
}