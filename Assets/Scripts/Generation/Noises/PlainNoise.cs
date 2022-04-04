public static class PlainNoise
{
    private static bool isInit = false; 
    
    private static FastNoiseLite noise;

    public static void SetupNoise(int seed)
    {
        noise = new FastNoiseLite(seed);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetFrequency(0.0075f);
        noise.SetFractalOctaves(4);
            
        isInit = true;
    }

    public static float GetNoise(float x, float y)
    {
        return noise.GetNoise(x, y);
    }
}
