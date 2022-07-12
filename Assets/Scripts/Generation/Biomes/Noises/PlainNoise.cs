
using Unity.Burst;
using UnityEngine;

[BurstCompile(CompileSynchronously = true)]
public class PlainNoise
{
    public static readonly FastNoiseLite noise = new FastNoiseLite()
    {
        mFractalType = FastNoiseLite.FractalType.FBm,
        mNoiseType = FastNoiseLite.NoiseType.OpenSimplex2,
        mFrequency = 0.0075f,
        mOctaves = 6,
        mFractalBounding = FastNoiseLite.CalculateFractalBounding(0, 6)
    };

    public static float GetNoise(int seed, float x, float y)
    {
        return noise.GetNoise(seed, x, y);
    }

}
