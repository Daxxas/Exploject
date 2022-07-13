using Unity.Burst;
using Unity.Mathematics;


[BurstCompile(CompileSynchronously = true)]
public class VanillaEquation : TerrainEquation
{ 
    public static readonly PlainNoise plainNoise = new PlainNoise();
    private static readonly TestNoise3D testNoise3D = new TestNoise3D();

    // public VanillaFunction(int seed)
    // {
    //     plainNoise = new PlainNoise(seed);
    //     testNoise3D = new TestNoise3D(seed);
    // }
    
    [BurstCompile(CompileSynchronously = true)]
    public static float GetResult(int seed, float x, float y, float z)
    {
        // return (-y / 64) + 1 + testNoise3D.GetNoise(x, y, z);
        
        return -y + 64 + (PlainNoise.GetNoise(seed, x/2, z/2) +1 / 2) * 30;
    }   
    
    public float GetResult(int seed, float3 xyz)
    {
        return GetResult(seed, xyz.x, xyz.y, xyz.z);
    } 
}