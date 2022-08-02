using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
 
public struct SampledNoiseCurve : System.IDisposable
{
    NativeArray<float> sampledFloat;
    /// <param name="samples">Must be 2 or higher</param>
    public SampledNoiseCurve(AnimationCurve ac, int samples)
    {
        sampledFloat = new NativeArray<float>(samples, Allocator.Persistent);
        float timeFrom = ac.keys[0].time;
        float timeTo = ac.keys[ac.keys.Length - 1].time;
        float timeStep = (timeTo - timeFrom) / (samples - 1);

        for (int i = 0; i < samples; i++)
        {
            sampledFloat[i] = ac.Evaluate(timeFrom + (i * timeStep));
        }
    }
 
    public void Dispose()
    {
        sampledFloat.Dispose();
    }
 
    /// <param name="value">Must be from -1 to 1</param>
    public float EvaluateLerp(float value)
    {
        int len = sampledFloat.Length - 1;
        value = math.clamp(value, -1, 1);
        float clamp01 = (value + 1) / 2;
        float floatIndex = ((clamp01) * len);
        int floorIndex = (int)math.floor(floatIndex);
        if (floorIndex == len)
        {
            return sampledFloat[len];
        }
 
        float lowerValue = sampledFloat[floorIndex];
        float higherValue = sampledFloat[floorIndex + 1];
        return math.lerp(lowerValue, higherValue, math.frac(floatIndex));
    }
}