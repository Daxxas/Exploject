using Unity.Mathematics;

public struct Triangle
{
    public Edge vertexIndexA;
    public Edge vertexIndexB;
    public Edge vertexIndexC;

    public bool isBorderA;
    public bool isBorderB;
    public bool isBorderC;

    public bool isBorderTriangle => isBorderA || isBorderB || isBorderC; 
    
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

    public bool GetIsBorder(int i)
    {
        switch (i)
        {
            case 0:
                return isBorderC;
            case 1:
                return isBorderB;
            default:
                return isBorderA;
        }
    }
    
    public void SetEdgeBorder(int i, bool value)
    {
        switch (i)
        {
            case 0:
                isBorderC = value;
                break;
            case 1:
                isBorderB = value;
                break;
            default:
                isBorderA = value;
                break;
        }
    }

    public Triangle(Edge a, Edge b, Edge c, bool isBorderA, bool isBorderB, bool isBorderC)
    {
        this.vertexIndexA = a;
        this.vertexIndexB = b;
        this.vertexIndexC = c;

        this.isBorderA = isBorderA;
        this.isBorderB = isBorderB;
        this.isBorderC = isBorderC;
    }
}