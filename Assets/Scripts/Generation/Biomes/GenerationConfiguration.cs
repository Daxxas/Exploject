using System;
using AOT;
using Sirenix.OdinInspector;
using Unity.Burst;
using UnityEngine;

[BurstCompile(CompileSynchronously = true)]
[CreateAssetMenu(fileName = "New Source Noise Config", menuName = "Biomes/Source Noise Config", order = 0)]
public class GenerationConfiguration : ScriptableObject
{
    [Header("yOffset")]
    [SerializeField] public FastNoiseLite yContinentalness;
    [SerializeField] public FastNoiseLite yPeaksValleys;
    [Space(10)]
    [Header("Squashiness")]
    [SerializeField] public FastNoiseLite squashContinentalness;
    [SerializeField] public FastNoiseLite squashPeaksValleys;
    [Header("Biomes")]
    [SerializeField] public FastNoiseLite humidity;
    [SerializeField] public FastNoiseLite temperature;
    
    [Space(10)]
    [HideInInspector] public AnimationCurve squashContinentCurve = new AnimationCurve();
    public SampledNoiseCurve sampledsquashContinentCurve;
    
    [HideInInspector] public AnimationCurve yContinentCurve = new AnimationCurve();
    public SampledNoiseCurve sampledyContinentCurve;
    
    
    private void OnValidate()
    {
        yContinentalness.UpdateFractalBounding();
        yPeaksValleys.UpdateFractalBounding();
        squashContinentalness.UpdateFractalBounding();
        squashPeaksValleys.UpdateFractalBounding();
        humidity.UpdateFractalBounding();
        temperature.UpdateFractalBounding();
    }
}