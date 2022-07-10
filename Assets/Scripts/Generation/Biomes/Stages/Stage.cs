using UnityEngine;

using Random = Unity.Mathematics.Random;
public abstract class Stage : ScriptableObject
{
    public FastNoiseLite noise;
    public int salt;
    
    public abstract ChunkBiome Apply(ChunkBiome chunkBiome);
}
