using System;
using System.Collections.Generic;
using Sirenix.Serialization;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "New Biome Pipeline", menuName = "Biomes/Pipeline", order = 0)]
public class BiomePipeline : ScriptableObject
{
    [SerializeField] private AnimationCurve continentalnessCurve = new AnimationCurve(new []{new Keyframe(-1, 0), new Keyframe(1, 100)});
    [SerializeField] private GenerationConfiguration generationConfiguration;
    public GenerationConfiguration GenerationConfiguration => generationConfiguration;
    public Gradient biomeGradient;
    public List<Biome> biomes = new List<Biome>();
    public float landBiomeThreshold;
    public float transitionRadius = 3;
    public float radiusFactor = 3;

    public List<BiomeHolder> biomeHolders = new List<BiomeHolder>();
    public List<BiomeHolder> BiomeHolders => biomeHolders;

    private void OnValidate()
    {
        biomeHolders = new List<BiomeHolder>();
        foreach (var biome in biomes)
        {
            biomeHolders.Add(biome.GetBiomeHolder());
        }
    }
}

