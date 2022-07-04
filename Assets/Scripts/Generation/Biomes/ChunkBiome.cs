using UnityEngine;

public class ChunkBiome
{
    public Vector2 origin;
    public int width;
    private int offset;
    public string[,] data;

    public string this[int i, int j]
    {
        get 
        {
            return this.data[i,j];   
        }
        set => this.data[i, j] = value;
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