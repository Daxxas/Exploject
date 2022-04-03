public class BaseNoiseFunction : IFunction
{
    public float GetResult(float x, float y, float z)
    {
        return (-y/64)+1 + noise.GetNoise( x, z);
    }
}
