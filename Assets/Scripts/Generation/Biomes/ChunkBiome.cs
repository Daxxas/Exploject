using UnityEngine;

public class ChunkBiome
{
    public Vector2 origin;
    public int width;
    private int offset;
    public int[,] data;

    public int this[int i, int j]
    {
        get
        {
            return this.data[i,j];   
        }
    }

    public static ChunkBiome Copy(ChunkBiome biome)
    {
        return new ChunkBiome()
        {
            origin = biome.origin,
            width = biome.width,
            offset = biome.offset,
            data = biome.data
        };
    }
    
    
}