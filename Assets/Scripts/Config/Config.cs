
using System;
using System.Collections.Generic;


public class Config
{
    public Dictionary<string, Noise> noise;
}

public class Noise : Function
{
    public int dimensions;
    public int octaves;

    public int GetDimensions()
    {
        return dimensions;
    }

    public int GetOctaves()
    {
        return octaves;
    }
}

public class Function
{
    public string type;
    public float frequency;
    public int min;
    public int max;
    public Function function;

    public FastNoiseLite.FractalType GetTypeAsFractal()
    {
        // Set Type
        switch (type.ToUpper())
        {
            case "FBM":
                return FastNoiseLite.FractalType.FBm;
                break;
            case "PINGPONG":
                return FastNoiseLite.FractalType.PingPong;
                break;
            case "Ridged":
                return FastNoiseLite.FractalType.Ridged;
                break;
            default:
                return FastNoiseLite.FractalType.FBm;
                break;
        }
    }
    
    public FastNoiseLite.NoiseType GetTypeAsNoise()
    {
        switch (type.ToUpper())
        {
            case "OPENSIMPLEX2":
                return FastNoiseLite.NoiseType.OpenSimplex2;
                break;
            case "OPENSIMPLEX2S":
                return FastNoiseLite.NoiseType.OpenSimplex2S;
                break;
            case "CELLULAR":
                return FastNoiseLite.NoiseType.Cellular;
                break;
            case "PERLIN":
                return FastNoiseLite.NoiseType.Perlin;
                break;
            case "VALUE":
                return FastNoiseLite.NoiseType.Value;
                break;
            case "VALUECUBIC":
                return FastNoiseLite.NoiseType.ValueCubic;
                break;
            default:
                return FastNoiseLite.NoiseType.OpenSimplex2;
                break;
        }
    }

    public string GetTypeStr()
    {
        return type;
    }
    
    public float GetFrequency()
    {
        return frequency;
    }

    public int GetMin()
    {
        return min;
    }
    
    
    public int GetMax()
    {
        return max;
    }
}
