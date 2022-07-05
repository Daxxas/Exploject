using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Random = Unity.Mathematics.Random;

[CreateAssetMenu(fileName = "New Expand Stage", menuName = "Biomes/Expand Stage", order = 3)]
public class Expand : Stage
{
    public FastNoiseLite noise;
    
    public override ChunkBiome Apply(ChunkBiome chunkBiome)
    {
        return chunkBiome.Expand(this);
    }

    public string GetMiddle(int x, int z, params string[] others)
    {
        return others[MathUtil.NormalizeIndex(noise.GetNoise(x,z), others.Length)];
    }
}