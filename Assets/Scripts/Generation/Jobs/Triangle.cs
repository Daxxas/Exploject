using Unity.Mathematics;

public struct Triangle
{
    public Edge vertexIndexA;
    public Edge vertexIndexB;
    public Edge vertexIndexC;
        
    public Edge this [int i] {
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
        set
        {
            switch (i) {
                case 0:
                    vertexIndexA = value;
                    break;
                case 1:
                    vertexIndexB = value;
                    break;
                default:
                    vertexIndexC = value;
                    break;
            }
        }
    }

    public Triangle(Edge a, Edge b, Edge c)
    {
        this.vertexIndexA = a;
        this.vertexIndexB = b;
        this.vertexIndexC = c;
    }
}