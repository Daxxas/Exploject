
using Unity.Mathematics;

public class MathUtil
{
    public static int NormalizeIndex(double val, int size)
    {
        return math.max(math.min((int)math.floor(((val + 1D) / 2D) * size), size - 1), 0);
    }
}