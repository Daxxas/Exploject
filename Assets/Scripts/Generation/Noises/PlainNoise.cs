public struct PlainNoise
{
    private FastNoiseLite noise;

    public PlainNoise(int seed)
    {
        noise = new FastNoiseLite(seed);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetFrequency(0.0075f);
        noise.SetFractalOctaves(6);
    }

    public float GetNoise(float x, float y)
    {
        return noise.GetNoise(x, y);
    }
}
