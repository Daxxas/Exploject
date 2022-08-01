public class WaveNoise
{
    private static bool isInit = false; 
    
    private static FastNoiseLite noise;

    public static void SetupNoise(int seed)
    {
        noise = new FastNoiseLite();
        noise.SetDomainWarpType(FastNoiseLite.DomainWarpType.OpenSimplex2);
        noise.SetDomainWarpAmp(20);
        noise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
        noise.SetFrequency(0.025f);
            
        isInit = true;
    }

    public static float GetNoise(int seed, float x, float y, float z)
    {
        return noise.GetNoise(seed, x, z);
    }
}
