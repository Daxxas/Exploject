using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class BiomeVisualizer : MonoBehaviour
{
    [SerializeField] private BiomeGenerator biomeGenerator;
    [SerializeField] private uint seed = 2;

    [ContextMenu("Generate Texture")]
    public void GenerateRandomTexture(bool random = true)
    {
        ChunkBiome chunkBiome;
        if (random)
        {
            chunkBiome = biomeGenerator.GetChunkBiome(Vector2.zero);
        }
        else
        {
            chunkBiome = biomeGenerator.GetChunkBiome(Vector2.zero, seed);
        }
        Texture2D texture = new Texture2D(chunkBiome.width,chunkBiome.width);
        texture.filterMode = FilterMode.Point;
        transform.localScale = new Vector3(chunkBiome.width, chunkBiome.width, 1);

        for (int x = 0; x < chunkBiome.width; x++)
        {
            for (int z = 0; z < chunkBiome.width; z++)
            {
                if (chunkBiome[x, z] == 0)
                {
                    texture.SetPixel(x,z, Color.blue);
                }
                else if (chunkBiome[x, z] == 1)
                {
                    texture.SetPixel(x,z, Color.green);
                }
                else
                {
                    texture.SetPixel(x,z, Color.magenta);
                }
            }
        }

        texture.Apply();

        GetComponent<Renderer>().sharedMaterial.mainTexture = texture;
    }


    private Random random;
    
    public ChunkBiome ApplyStep(ChunkBiome chunkBiome, int index)
    {
        if (index == 0)
        {
            random = new Random(seed);
            chunkBiome = biomeGenerator.InitChunk(Vector2.zero, ref random);
        }
        else
        {
            chunkBiome = biomeGenerator.ApplyStage(chunkBiome, ref random, index);
        }

        Texture2D texture = new Texture2D(chunkBiome.width,chunkBiome.width);
        texture.filterMode = FilterMode.Point;
        transform.localScale = new Vector3(chunkBiome.width, chunkBiome.width, 1);

        for (int x = 0; x < chunkBiome.width; x++)
        {
            for (int z = 0; z < chunkBiome.width; z++)
            {
                if (chunkBiome[x, z] == 0)
                {
                    texture.SetPixel(x,z, Color.blue);
                }
                else if (chunkBiome[x, z] == 1)
                {
                    texture.SetPixel(x,z, Color.green);
                }
                else
                {
                    texture.SetPixel(x,z, Color.magenta);
                }
            }
        }
        
        texture.Apply();
        GetComponent<Renderer>().sharedMaterial.mainTexture = texture;
        
        return chunkBiome;
    }

    public void ResetVisualization()
    {
        transform.localScale = Vector3.one;
        GetComponent<Renderer>().sharedMaterial.mainTexture = null;
    }
        
}