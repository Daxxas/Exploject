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
    [SerializeField] private int2 offset = int2.zero;

    
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
                Color pixelColor = chunkBiome[x, z].color;

                texture.SetPixel(x,z, pixelColor);
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