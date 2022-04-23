
using Unity.Mathematics;

public struct VanillaFunction
{
    private PlainNoise plainNoise;

    public VanillaFunction(int seed)
    {
        plainNoise = new PlainNoise(seed);
    }
    
    public float GetResult(float x, float y, float z)
    {
        return -y + 64 + (plainNoise.GetNoise(x/2, z/2) +1 / 2) * 10;

        return (-y +64) * ((plainNoise.GetNoise(x, z) + 1) / 2 * 10);
    }   
    
    public float GetResult(float3 xyz)
    {
        return GetResult(xyz.x, xyz.y, xyz.z);
    } 
}
