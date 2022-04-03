using org.mariuszgromada.math.mxparser;

public class FunctionNoise2D : FunctionExtension
{
    private float x;
    private float y;
    private FastNoiseLite noise;
    
    public FunctionNoise2D()
    {
        x = float.NaN;
        y = float.NaN;
    }
    
    public FunctionNoise2D(float x, float y, FastNoiseLite noise)
    {
        this.x = x;
        this.y = y;
        this.noise = noise;
    }
    
    public FunctionNoise2D(FastNoiseLite noise)
    {
        this.noise = noise;
    }
    
    public int getParametersNumber()
    {
        return 2;
    }

    public void setParameterValue(int parameterIndex, double parameterValue)
    {
        if (parameterIndex == 0) x = (float) parameterValue;
        if (parameterIndex == 1) y = (float) parameterValue;
    }

    public string getParameterName(int parameterIndex)
    {
        if (parameterIndex == 0) return "x";
        return "y";
    }

    public double calculate()
    {
        return noise.GetNoise(x, y);
    }

    public FunctionExtension clone()
    {
        return new FunctionNoise2D(x,y,noise);
    }
}


public class FunctionNoise3D : FunctionExtension
{
    private float x;
    private float y;
    private float z;
    private FastNoiseLite noise;
    
    public FunctionNoise3D()
    {
        x = float.NaN;
        y = float.NaN;
    }

    public FunctionNoise3D(float x, float y, float z, FastNoiseLite noise)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.noise = noise;
    }
    
    public FunctionNoise3D(FastNoiseLite noise)
    {
        this.noise = noise;
    }
    
    public int getParametersNumber()
    {
        return 2;
    }

    public void setParameterValue(int parameterIndex, double parameterValue)
    {
        if (parameterIndex == 0) x = (float) parameterValue;
        if (parameterIndex == 1) y = (float) parameterValue;
        if (parameterIndex == 2) z = (float) parameterValue;
    }

    public string getParameterName(int parameterIndex)
    {
        if (parameterIndex == 0) return "x";
        if (parameterIndex == 1) return "y";
        return "z";
    }

    public double calculate()
    {
        return noise.GetNoise(x, y, z);
    }

    public FunctionExtension clone()
    {
        return new FunctionNoise3D(x,y,z,noise);
    }
}
