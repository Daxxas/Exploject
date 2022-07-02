using UnityEngine;

using Random = Unity.Mathematics.Random;
public abstract class Stage : ScriptableObject
{
    public abstract ChunkBiome Apply(int x, int z, ChunkBiome chunkBiome, ref Random r);
}