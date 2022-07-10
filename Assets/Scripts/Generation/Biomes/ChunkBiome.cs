using UnityEngine;

public class ChunkBiome
{
    public Vector2 origin;
    public int width;
    private int offset;
    public Biome[,] data;

    public Biome this[int i, int j]
    {
        get 
        {
            return this.data[i,j];   
        }
        set => this.data[i, j] = value;
    }
    
    public ChunkBiome(int width, Vector2 origin) {
        width += 4;
        this.width = width;
        data = new Biome[width,width];
        this.origin = origin;
        this.offset = 2;
    }
    private ChunkBiome(Biome[,] data, Vector2 origin, int width, int offset) {
        this.data = data;
        this.origin = origin;
        this.width = width;
        this.offset = 2 * offset;
    }
    
    public ChunkBiome Expand(Expand expander)
    {
        Biome[,] old = data;
        int newWidth = width * 2 - 1;

        data = new Biome[newWidth, newWidth];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < width; z++)
            {
                data[x * 2, z * 2] = old[x, z];

                if(z != width - 1)
                {
                    data[x * 2, z * 2 + 1] = expander.GetMiddle(x + (int)origin.x, z + 1 + (int)origin.y,old[x, z], old[x, z + 1]);
                }
        
                if (x != width - 1)
                {
                    data[x * 2 + 1, z * 2] = expander.GetMiddle(x+1 + (int)origin.x,z + (int)origin.y,old[x, z], old[x + 1, z]);
                }
        
                if (x != width - 1 && z != width - 1)
                {
                    data[x * 2 + 1, z * 2 + 1] = expander.GetMiddle(x+1 + (int)origin.x,z+1 + (int)origin.y, old[x, z], old[x + 1, z], old[ x + 1, z + 1], old[x, z + 1]);
                }
                
            }
        }
        
        Vector2 newOrigin = new Vector2(origin.x * 2 - 1, origin.y * 2 - 1);
        
        return new ChunkBiome(data, newOrigin, newWidth, offset);
    }
}