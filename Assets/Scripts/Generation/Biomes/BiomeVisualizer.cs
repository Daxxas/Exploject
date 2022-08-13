using System;
using System.Collections;
using Sirenix.OdinInspector;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class BiomeVisualizer : MonoBehaviour
{
    [SerializeField] private BiomeGenerator biomeGenerator;
    [SerializeField] private int seed = 2;
    [SerializeField] private int previewSize = 3;
    [SerializeField] private int2 testpos;

    [ContextMenu("Test Biome At Pos")]
    public void TestBiomeAtPos()
    {
        var biome = biomeGenerator.GetBiomeAtPos(testpos);
        Texture2D texture = (Texture2D) GetComponent<Renderer>().sharedMaterial.mainTexture;
        texture.SetPixel(testpos.x, testpos.y, Color.magenta);
        texture.Apply();
        Debug.Log(biome);    
    }
    
    [Button]
    public void GenerateRandomTexture()
    {
        GenerationInfo.Seed = seed;
        // int textureSize = previewSize;
        int textureSize = previewSize * MapDataGenerator.SupportedChunkSize;
        Texture2D texture = new Texture2D(textureSize,textureSize);
        texture.filterMode = FilterMode.Point;



        for (int biomeX = 0; biomeX < previewSize; biomeX++)
        {
            for (int biomeZ = 0; biomeZ < previewSize; biomeZ++)
            {
                BiomeHolder[] biomes = biomeGenerator.GetBiomesForChunk(new Vector2(testpos.x + biomeX, testpos.y + biomeZ), 1);
                
                for (int x = 0; x < MapDataGenerator.SupportedChunkSize; x++)
                {
                    for (int z = 0; z < MapDataGenerator.SupportedChunkSize; z++)
                    {
                        int biomeIdx =  x + MapDataGenerator.SupportedChunkSize * z;
                        
                        Color pixelColor = new Color(biomes[biomeIdx].color.x, biomes[biomeIdx].color.y, biomes[biomeIdx].color.z, biomes[biomeIdx].color.w);
                        texture.SetPixel(x + (biomeX * MapDataGenerator.SupportedChunkSize),z + (biomeZ * MapDataGenerator.SupportedChunkSize), pixelColor);
                    }
                }
            }
        }

        texture.Apply();
    
        GetComponent<Renderer>().sharedMaterial.mainTexture = texture;
    }

    [Button]
    public void ResetVisualization()
    {
        transform.localScale = Vector3.one;
        GetComponent<Renderer>().sharedMaterial.mainTexture = null;
    }
}