using AOT;
using Unity.Burst;

[BurstCompile(CompileSynchronously = true)]
public class OceanEquation : TerrainEquation
{
    [BurstCompile(CompileSynchronously = true)]
    [MonoPInvokeCallback(typeof(TerrainEquationDelegate))]
    public static float GetResult(int seed, float x, float y, float z)
    {
        return 10 - y;
    }   
}