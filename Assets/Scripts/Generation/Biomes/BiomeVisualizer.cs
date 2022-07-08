using System;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class BiomeVisualizer : MonoBehaviour
{
    [SerializeField] private BiomeGenerator biomeGenerator;
    [SerializeField] private int seed = 2;
    [SerializeField] private Vector2 offset = Vector2.zero;

    
    [ContextMenu("Generate Texture")]
    public void GenerateRandomTexture()
    {
        GenerationInfo.Seed = seed;
        ChunkBiome chunkBiome = biomeGenerator.GetChunkBiome(offset, GenerationInfo.Seed);
        Texture2D texture = new Texture2D(chunkBiome.width,chunkBiome.width);
        texture.filterMode = FilterMode.Point;
        
        for (int x = 0; x < chunkBiome.width; x++)
        {
            for (int z = 0; z < chunkBiome.width; z++)
            {
                // Color pixelColor = biomeGenerator.Pipeline.initialBiomes.GetBiomeFromId(chunkBiome[x, z]).color;
                //
                // texture.SetPixel(x,z, pixelColor);
            }
        }
    
        texture.Apply();
    
        GetComponent<Renderer>().sharedMaterial.mainTexture = texture;
    }


    private FastNoiseLite noise;
    
    public ChunkBiome ApplyStep(ChunkBiome chunkBiome, int index)
    {
        if (index == 0)
        {
            noise = new FastNoiseLite()
            {
                mSeed = seed,
                mNoiseType = FastNoiseLite.NoiseType.WhiteNoise,
                mFrequency = 1f
            };
            // chunkBiome = biomeGenerator.InitChunk(Vector2.zero, GenerationInfo.GetRandomSeed());
        }
        else
        {
            chunkBiome = biomeGenerator.ApplyStage(chunkBiome, index);
        }

        Texture2D texture = new Texture2D(chunkBiome.width,chunkBiome.width);
        texture.filterMode = FilterMode.Point;
        transform.localScale = new Vector3(chunkBiome.width, chunkBiome.width, 1);

        for (int x = 0; x < chunkBiome.width; x++)
        {
            for (int z = 0; z < chunkBiome.width; z++)
            {
                // if (chunkBiome[x, z] == 0)
                // {
                //     texture.SetPixel(x,z, Color.blue);
                // }
                // else if (chunkBiome[x, z] == 1)
                // {
                //     texture.SetPixel(x,z, Color.green);
                // }
                // else
                // {
                //     texture.SetPixel(x,z, Color.magenta);
                // }
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