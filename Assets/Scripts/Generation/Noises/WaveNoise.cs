public class WaveNoise
{
    private static bool isInit = false; 
    
    private static FastNoiseLite noise;

    public static void SetupNoise(int seed)
    {
        noise = new FastNoiseLite(seed);
        noise.SetDomainWarpType(FastNoiseLite.DomainWarpType.OpenSimplex2);
        noise.SetDomainWarpAmp(20);
        noise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
        noise.SetFrequency(0.025f);
            
        isInit = true;
    }

    public static float GetNoise(float x, float y, float z)
    {
        return noise.GetNoise(x, z);
    }
}
