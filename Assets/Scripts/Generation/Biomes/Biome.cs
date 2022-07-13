using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Sirenix.OdinInspector;
using Unity.Collections;
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
    
    [HideIf("isSelf")] [SerializeField] 
    private List<string> serializedTags = new List<string>();
    public HashSet<string> tags = new HashSet<string>();

    public Type test;

    public BiomeHolder BuildBiome()
    {
        NativeParallelHashSet<int> burstTags = new NativeParallelHashSet<int>(tags.Count, Allocator.Persistent);
        foreach (string tag in tags)
        {
            burstTags.Add(Convert.ToInt32(Encoding.ASCII.GetBytes(tag)));
        }
        
        return new BiomeHolder(Convert.ToInt32(Encoding.ASCII.GetBytes(id)), new float3(color.r, color.g, color.b), burstTags);
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

public struct BiomeHolder
{
    public BiomeHolder(int id, float3 color, NativeParallelHashSet<int> tags)
    {
        this.id = id;
        this.color = color;
        this.tags = tags;
    }

    public int id;
    public float3 color;
    public NativeParallelHashSet<int> tags;
}
