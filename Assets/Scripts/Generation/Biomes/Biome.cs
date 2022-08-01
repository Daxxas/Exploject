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
public class Biome : ScriptableObject, ISerializationCallbackReceiver
{
    public bool isSelf = false;
    
    [HideIf("isSelf")] [SerializeField]
    private string id;
    public string Id => id;
    
    [HideIf("isSelf")] 
    public Color color;

    public Material biomeMaterial;
    
    [HideIf("isSelf")]
    [SerializeField] private List<string> serializedTags = new List<string>();
    public HashSet<string> tags = new HashSet<string>();

    
    
    public BiomeHolder GetBiomeHolder()
    {
        return new BiomeHolder( new FixedString32Bytes(id), new float3(color.r, color.g, color.b));;
    }
    
    public void OnBeforeSerialize()
    {
        tags = new HashSet<string> (serializedTags);
        tags.Add(Id);

        foreach (var val in tags) {
            if (!serializedTags.Contains (val)) {
                serializedTags.Add (val);
            }
        } 
    }

    public void OnAfterDeserialize()
    {
        tags.Clear();

        foreach (var val in serializedTags) {
            tags.Add (val);
        }
    }

    public override string ToString()
    {
        return $"{Id}";
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
