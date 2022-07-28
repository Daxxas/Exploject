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
    public List<Stage> stages;
}

