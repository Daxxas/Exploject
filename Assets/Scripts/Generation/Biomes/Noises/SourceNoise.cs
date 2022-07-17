using UnityEngine;

[CreateAssetMenu(fileName = "New Source Noise Config", menuName = "Biomes/Source Noise Config", order = 0)]
public class SourceNoise : ScriptableObject
{
    [SerializeField] public FastNoiseLite continentalness;
    [SerializeField] public FastNoiseLite peaksvalleys;
    [SerializeField] public FastNoiseLite humidity;
    [SerializeField] public FastNoiseLite temperature;
}