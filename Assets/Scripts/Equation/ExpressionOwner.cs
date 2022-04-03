public static class ExpressionOwner
{
    private static FastNoiseLite noise;
    private static FastNoiseLite noiseWarp;

    public static void SetupNoise(int seed)
    {
        noise = new FastNoiseLite(seed);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetFrequency(0.0075f);
        noise.SetFractalOctaves(4);

        noiseWarp = new FastNoiseLite(seed);
        noiseWarp.SetDomainWarpType(FastNoiseLite.DomainWarpType.OpenSimplex2);
        noiseWarp.SetDomainWarpAmp(20);
        noiseWarp.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
        noiseWarp.SetFrequency(0.025f);
    }

    public static double Noise2(double x, double y)
    {
        return noise.GetNoise((float) x,(float) y);
    }

    public static double Desert(double x, double y)
    {
        return noiseWarp.GetNoise((float) x,(float) y);
    }
}
