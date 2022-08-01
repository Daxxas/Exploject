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
    
    [ContextMenu("Generate Texture")]
    public void GenerateRandomTexture()
    {
        GenerationInfo.Seed = seed;
        int textureSize = previewSize;
        Texture2D texture = new Texture2D(textureSize,textureSize);
        texture.filterMode = FilterMode.Point;

        for (int x = 0; x < previewSize; x++)
        {
            for (int z = 0; z < previewSize; z++)
            {
                Color pixelColor = biomeGenerator.GetBiomeAtPos(new int2(x, z)).color;
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