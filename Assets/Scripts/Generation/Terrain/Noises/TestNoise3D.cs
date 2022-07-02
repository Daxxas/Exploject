public struct TestNoise3D
{
    
    private FastNoiseLite noise;

    public TestNoise3D(int seed)
    {
        noise = new FastNoiseLite(seed)
        {
            mNoiseType = FastNoiseLite.NoiseType.OpenSimplex2,
            mTransformType3D = FastNoiseLite.TransformType3D.DefaultOpenSimplex2,
            mFractalType = FastNoiseLite.FractalType.FBm,
            mOctaves = 4
        };
    }

    public float GetNoise(float x, float y, float z)
    {
        return noise.GetNoise(x, y, z);
    }
}
