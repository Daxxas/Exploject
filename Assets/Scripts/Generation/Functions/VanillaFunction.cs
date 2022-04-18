
public struct VanillaFunction
{
    private TestNoise3D testNoise3D;

    public VanillaFunction(int seed)
    {
        testNoise3D = new TestNoise3D(seed);
    }
    
    public float GetResult(float x, float y, float z)
    {
        return (-y / 64) + 1 + testNoise3D.GetNoise(x, y, z);
        // return (-y / 64) + 1 + PlainNoise.GetNoise(x, z);
    }   
}
