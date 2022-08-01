using AOT;
using Unity.Burst;

[BurstCompile(CompileSynchronously = true)]
public class MountainEquation : TerrainEquation
{
    [BurstCompile(CompileSynchronously = true)]
    [MonoPInvokeCallback(typeof(TerrainEquationDelegate))]
    public static float GetResult(int seed, float x, float y, float z)
    {
        return 0;
    }
}