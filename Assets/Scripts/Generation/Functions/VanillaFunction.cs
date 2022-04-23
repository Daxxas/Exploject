
using Unity.Mathematics;

public struct VanillaFunction
{
    private PlainNoise plainNoise;
    private TestNoise3D testNoise3D;

    public VanillaFunction(int seed)
    {
        plainNoise = new PlainNoise(seed);
        testNoise3D = new TestNoise3D(seed);
    }
    
    public float GetResult(float x, float y, float z)
    {
        return (-y / 64) + 1 + testNoise3D.GetNoise(x, y, z);
        
        // return -y + 64 + (testNoise3D.GetNoise(x/2, y/2, z/2) + 1 / 2) * 10;
    }   
    
    public float GetResult(float3 xyz)
    {
        return GetResult(xyz.x, xyz.y, xyz.z);
    } 
}
