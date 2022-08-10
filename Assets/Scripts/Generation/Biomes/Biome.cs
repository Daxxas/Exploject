using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Sirenix.OdinInspector;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;


[CreateAssetMenu(fileName = "New Biome", menuName = "Biomes/Biome", order = 1)]
public class Biome : ScriptableObject
{
    
    [SerializeField]
    private string id;
    public string Id => id;
    
    public Color color;

    public Material biomeMaterial;
    [SerializeField] [NonReorderable] public BiomeFeature[] features;

    public BiomeHolder GetBiomeHolder()
    {
        return new BiomeHolder( new FixedString32Bytes(id), new float3(color.r, color.g, color.b));;
    }

    public override string ToString()
    {
        return $"{Id}";
    }
    
    public void OnValidate()
    {
        if(features == null) return;
        
        for (int i = 0; i < features.Length; i++)
        {
            features[i].distribution.UpdateFractalBounding();
        }
    }
}

[Serializable]
public struct BiomeFeature
{
    [SerializeField] public GameObject feature;
    [SerializeField] public LayerMask layerMask;
    [SerializeField] public float minHeight;
    [SerializeField] public float maxHeight;
    [SerializeField] public float step;
    [SerializeField] public Vector3 spawnOffset;
    [SerializeField] public FastNoiseLite distribution;

    public void GenerateFeature(Transform parent, Vector3 spawnPosition)
    {
        float featureRotation = GenerationInfo.FeatureRotationNoise.GetNoise(GenerationInfo.seed, spawnPosition.x, spawnPosition.z);
        featureRotation = (featureRotation + 1) / 2;
        featureRotation *= 360;
                                
        GameObject featureObject = GameObject.Instantiate(feature, spawnPosition + spawnOffset, Quaternion.Euler(0, featureRotation, 0));
        featureObject.transform.parent = parent;
    }
}

[BurstCompile(CompileSynchronously = true)]
public struct BiomeHolder : IEquatable<BiomeHolder>
{
    public BiomeHolder(FixedString32Bytes id, float3 color) //, UnsafeParallelHashSet<int> tags)
    {
        this.id = id;
        this.color = color;
        // this.tags = tags;
    }
    
    public FixedString32Bytes id;
    public float3 color;
    // public UnsafeParallelHashSet<int> tags;

    public bool Equals(BiomeHolder other)
    {
        return id == other.id;
    }

    public override bool Equals(object obj)
    {
        return obj is BiomeHolder other && Equals(other);
    }

    public override int GetHashCode()
    {
        return id.GetHashCode();
    }
}
