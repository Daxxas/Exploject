using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
 
public struct SampledGradient : System.IDisposable
{
    NativeArray<float4> sampledColor;
    public SampledGradient(Gradient gradient, int samples)
    {
        sampledColor = new NativeArray<float4>(samples, Allocator.Persistent);
        float timeStep = 1 / ((float) samples - 1);

        for (int i = 0; i < samples; i++)
        {
            Color evaluatedColor = gradient.Evaluate(i * timeStep);
            sampledColor[i] = new float4(evaluatedColor.r, evaluatedColor.g, evaluatedColor.b, evaluatedColor.a);
        }
    }
 
    public void Dispose()
    {
        sampledColor.Dispose();
    }
 
    /// <param name="value">Must be from -1 to 1</param>
    public float4 EvaluateLerp(float value)
    {
        int len = sampledColor.Length - 1;
        value = math.clamp(value, -1, 1);
        float clamp01 = (value + 1) / 2;
        float floatIndex = ((clamp01) * len);
        int floorIndex = (int)math.floor(floatIndex);
        if (floorIndex == len)
        {
            return sampledColor[len];
        }
 
        float4 lowerValue = sampledColor[floorIndex];
        float4 higherValue = sampledColor[floorIndex + 1];
        return (lowerValue + higherValue) / 2;
    }
}