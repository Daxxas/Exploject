using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Biome Pipeline", menuName = "Biomes/Pipeline", order = 0)]
public class BiomePipeline : ScriptableObject
{
    public int initialSize = 3;
    
    // TODO : Have weighted lists
    public List<Biome> initialBiomes;

    public List<Stage> stages;
}