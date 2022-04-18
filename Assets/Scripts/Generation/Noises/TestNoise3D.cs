public struct TestNoise3D
{
    
    private FastNoiseLite noise;

    public TestNoise3D(int seed)
    {
        noise = new FastNoiseLite();
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetFrequency(0.0075f);
        noise.SetFractalOctaves(4);
    }

    public float GetNoise(float x, float y, float z)
    {
        return noise.GetNoise(x, y, z);
    }
}
