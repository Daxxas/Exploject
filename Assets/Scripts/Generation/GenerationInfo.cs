

using Unity.Mathematics;

public static class GenerationInfo
{
    public static int seed = 1334;

    public static int Seed
    {
        get => seed;
        set => seed = value;
    }

    public static int GetRandomSeed()
    {
        Random r = new Random();
        r.InitState();
        return r.NextInt();
    }
}