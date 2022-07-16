using System;
using System.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class BiomeVisualizer : MonoBehaviour
{
    [SerializeField] private BiomeGenerator biomeGenerator;
    [SerializeField] private int seed = 2;
    [SerializeField] private int chunksPreview = 3;
    [SerializeField] private int2 testpos;

    [ContextMenu("Test Biome At Pos")]
    public void TestBiomeAtPos()
    {
        Debug.Log(biomeGenerator.Pipeline.GetChunkBiomeSize());
        
        var biome = biomeGenerator.GetBiomeAtPos(testpos);
        Texture2D texture = (Texture2D) GetComponent<Renderer>().sharedMaterial.mainTexture;
        texture.SetPixel(testpos.x, testpos.y, Color.magenta);
        texture.Apply();
        Debug.Log(biome);    
    }
    
    [ContextMenu("Generate Texture")]
    public void GenerateRandomTexture()
    {
        biomeGenerator.ResetBiomeDictionnary();
        
        GenerationInfo.Seed = seed;
        int chunkBiomeSize = biomeGenerator.Pipeline.GetChunkBiomeSize();
        int textureSize = chunksPreview * chunkBiomeSize;
        Texture2D texture = new Texture2D(textureSize,textureSize);
        texture.filterMode = FilterMode.Point;

        for (int chunkX = 0; chunkX < chunksPreview; chunkX++)
        {
            for (int chunkZ = 0; chunkZ < chunksPreview; chunkZ++)
            {
                ChunkBiome chunkBiome = biomeGenerator.GetChunkBiome(new int2(chunkX, chunkZ), GenerationInfo.Seed);
                
                for (int x = 0; x < chunkBiome.width; x++)
                {
                    for (int z = 0; z < chunkBiome.width; z++)
                    {
                        Color pixelColor = chunkBiome[x, z].color;
                        texture.SetPixel(x + (chunkX * chunkBiomeSize),z + (chunkZ * chunkBiomeSize), pixelColor);
                    }
                }
            }
        }
        
    
        texture.Apply();
    
        GetComponent<Renderer>().sharedMaterial.mainTexture = texture;
    }

    public void ResetVisualization()
    {
        transform.localScale = Vector3.one;
        GetComponent<Renderer>().sharedMaterial.mainTexture = null;
    }
}