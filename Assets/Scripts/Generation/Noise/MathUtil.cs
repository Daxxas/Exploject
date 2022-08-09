
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile(CompileSynchronously = true)]
public class MathUtil
{
    [BurstCompile(CompileSynchronously = true)]
    public static int NormalizeIndex(double val, int size)
    {
        return math.max(math.min((int)math.floor(((val + 1D) / 2D) * size), size - 1), 0);
    }

    [BurstCompile(CompileSynchronously = true)]
    public static int to1D( int x, int y, int z, int supportedChunkSize, int chunkHeight)
    {
        return x + y*supportedChunkSize + z*supportedChunkSize*chunkHeight;
    }
}