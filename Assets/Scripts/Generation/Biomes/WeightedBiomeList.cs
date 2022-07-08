
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

[Serializable]
public class WeightedBiomeList : IList<WeightedBiome>
{
    [NonSerialized, HideInInspector]
    private Dictionary<string, WeightedBiome> biomes = new Dictionary<string, WeightedBiome>();
    private List<WeightedBiome> biomesList = new List<WeightedBiome>();
    private int TotalWeight()
    {
        int total = 0;
        foreach (var biome in this)
        {
            total += biome.weight;
        }
        return total;
    }

    public void UpdateDictionnary()
    {
        biomes.Clear();
        foreach (var biome in biomesList)
        {
            if(biome.biome != null)
                biomes.Add(biome.biome.Id, biome);
        }
    }

    public Biome GetRandomBiome(FastNoiseLite noise, int x, int z)
    {
        int randomNumber = MathUtil.NormalizeIndex(noise.GetNoise(x, z), TotalWeight());
        
        WeightedBiome selectedBiome = this[0];

        foreach (var weightedBiome in this)
        {
            if (randomNumber < weightedBiome.weight)
            {
                selectedBiome = weightedBiome;
                break;
            }

            randomNumber -= weightedBiome.weight;
        }

        return selectedBiome.biome;
    }

    public Biome GetBiomeFromId(string id)
    {
        return biomes[id].biome;
    }

    public IEnumerator<WeightedBiome> GetEnumerator()
    {
        foreach (var biome in biomesList)
        {
            yield return biome;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(WeightedBiome item)
    {
        // biomes.Add(item.biome.Id, item);
        biomesList.Add(item);
    }

    public void Clear()
    {
        biomes.Clear();
        biomesList.Clear();
    }

    public bool Contains(WeightedBiome item)
    {
        return biomesList.Contains(item);
    }

    public void CopyTo(WeightedBiome[] array, int arrayIndex)
    {
        biomesList.CopyTo(array, arrayIndex);
    }

    public bool Remove(WeightedBiome item)
    {
        return biomesList.Remove(item);
    }

    public int Count { get => biomesList.Count; }
    public bool IsReadOnly { get => false; }
    public int IndexOf(WeightedBiome item)
    {
        return biomesList.IndexOf(item);
    }

    public void Insert(int index, WeightedBiome item)
    {
        biomesList.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        biomesList.RemoveAt(index);
    }

    public WeightedBiome this[int index]
    {
        get => biomesList[index];
        set => biomesList[index] = value;
    }
}

[Serializable]
public class UnitySerializedDictionary : ISerializationCallbackReceiver
{
    [SerializeField]
    private List<WeightedBiome> valueList = new ();

    [SerializeField]
    int testvalue = 2;
    
    [NonSerialized]
    private Dictionary<string, WeightedBiome> valueDic = new ();
    
    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        valueList.Clear();
        foreach (var item in valueDic)
        {
            // this.valueData.Add(item.Value);
            valueList.Add(item.Value);
        }
    }
    
    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
        this.valueDic.Clear();
    
        foreach (var item in valueList)
        {
            this.valueDic.Add(item.biome.Id, item);
        }
    }
}

[Serializable]
public class WeightedBiome
{
    public Biome biome;
    public int weight;
}