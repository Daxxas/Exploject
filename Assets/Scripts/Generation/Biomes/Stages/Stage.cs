using UnityEngine;

public abstract class Stage : ScriptableObject
{
    public FastNoiseLite noise;
    public int salt;
    
    public abstract ChunkBiome Apply(ChunkBiome chunkBiome, int seed);
}
