using Unity.Mathematics;

public struct Triangle
{
    public int3 vertexIndexA;
    public int3 vertexIndexB;
    public int3 vertexIndexC;
        
    public int3 this [int i] {
        get {
            switch (i) {
                case 0:
                    return vertexIndexA;
                case 1:
                    return vertexIndexB;
                default:
                    return vertexIndexC;
            }
        }
    }

    public Triangle(int3 a, int3 b, int3 c)
    {
        this.vertexIndexA = a;
        this.vertexIndexB = b;
        this.vertexIndexC = c;
    }
}