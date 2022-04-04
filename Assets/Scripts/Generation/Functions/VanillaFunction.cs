
public class VanillaFunction
{
    public static float GetResult(float x, float y, float z)
    {
        return (-y / 64) + 1 + PlainNoise.GetNoise(x, z);
    }   
}
