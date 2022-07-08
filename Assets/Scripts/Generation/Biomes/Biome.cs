using System;
using UnityEngine;


[CreateAssetMenu(fileName = "New Biome", menuName = "Biomes/Biome", order = 1)]
public class Biome : ScriptableObject
{
    private string id;
    public string Id => id;
    
    public Color color;
}
