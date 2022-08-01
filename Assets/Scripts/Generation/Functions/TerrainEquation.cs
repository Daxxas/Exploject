
using Unity.Burst;

[BurstCompile(CompileSynchronously = true)]
public class TerrainEquation
{
    public delegate float TerrainEquationDelegate(int seed, float x, float y, float z);

}