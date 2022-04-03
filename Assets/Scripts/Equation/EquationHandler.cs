
using System;
using System.Collections.Generic;
using Flee.PublicTypes;
using org.mariuszgromada.math.mxparser;
using UnityEngine;
using Function = org.mariuszgromada.math.mxparser.Function;

public class EquationHandler
{
    private string currentEquation;

    private float currentX;
    private float currentY;
    private float currentZ;

    #region flee

    private IDynamicExpression e;
    private ExpressionContext context;

    public EquationHandler(string equation, int seed)
    {
        equation = PlaceAbsolute(equation);
        equation = PlaceDefaultIfCondition(equation);
        
        Debug.Log(equation);
        
        ExpressionOwner.SetupNoise(seed);
        
        context = new ExpressionContext();
        context.ParserOptions.FunctionArgumentSeparator = ',';
        context.ParserOptions.DecimalSeparator = '.';
        
        context.ParserOptions.RecreateParser();

        context.Imports.AddType(typeof(ExpressionOwner));
        context.Imports.AddType(typeof(Math));

        context.Variables["x"] = (double) 0;
        context.Variables["y"] = (double) 0;
        context.Variables["z"] = (double) 0;
        
        
        e = context.CompileDynamic(equation);
    }

    public float EquationResult(double x, double y, double z)
    {
        context.Variables["x"] = x;
        context.Variables["y"] = y;
        context.Variables["z"] = z;

        var result = (double) e.Evaluate();

        return (float) result;
    }


#endregion

    private string PlaceAbsolute(string equation)
    {
        bool openBar = true;
        string returnString = equation;
        
        int i = 0;
        while(i < returnString.Length)
        {
            if (returnString[i] == '|')
            {
                if (openBar)
                {
                    returnString = returnString.Remove(i, 1);
                    returnString = returnString.Insert(i, "abs(");
                }
                else
                {
                    returnString = returnString.Remove(i, 1);
                    returnString = returnString.Insert(i, ")");
                }
                openBar = !openBar;
            }

            i++;
        }

        return returnString;
    }

    private string PlaceDefaultIfCondition(string equation)
    {
        string defaultCondition = " <>0";

        bool openParenthesis = false; // need to count them to handle case like (((abc))abc(abc))
        bool inIfCondition = false;
        bool alreadyHasCondition = false;
        
        string returnString = equation;

        int i = 0;
        
        while(i < returnString.Length)
        {
            if (inIfCondition)
            {
                if (returnString[i] == ')')
                {
                    openParenthesis = false;
                }

                if (alreadyHasCondition && returnString[i] != ',')
                {
                    i++;
                    continue;
                }
                else
                {
                    alreadyHasCondition = false;
                }
            
                if (openParenthesis)
                {
                    i++;
                    continue;
                }
            
                if (returnString[i] == '(')
                {
                    openParenthesis = true;
                }
                else if (returnString[i] == '<' || returnString[i] == '>' || returnString[i] == '=')
                {
                    alreadyHasCondition = true;
                }
                else if (returnString[i] == ',')
                {
                    returnString = returnString.Insert(i, defaultCondition);
                    i += defaultCondition.Length;
                    inIfCondition = false;
                }
            }
            else
            {
                if (i + 3 < returnString.Length)
                {
                    if (returnString.Substring(i, 3) == "if(")
                    {
                        inIfCondition = true;
                        i += 3;
                    }
                }
            }

            i++;
        }


        if (openParenthesis)
        {
            throw new Exception("if condition contains non closed parenthesis");
            return null;
        }
        else
        {
            return returnString;
        }
    }

    #region mxparser
    
    // private Expression e;

    // public EquationHandler(string equation, Dictionary<string, EquationNoise> noiseList)
    // {
    //     Argument argX = new Argument("x", 0);
    //     Argument argY = new Argument("y", 0);
    //     Argument argZ = new Argument("z", 0);
    //     
    //     PrimitiveElement[] elements = new PrimitiveElement[noiseList.Count + 3];
    //
    //     elements[0] = argX;
    //     elements[1] = argY;
    //     elements[2] = argZ;
    //     
    //     int count = 3;
    //     foreach (var noiseElement in noiseList)
    //     {
    //         if (noiseElement.Value.dimension == 2)
    //         {
    //             org.mariuszgromada.math.mxparser.Function f = new org.mariuszgromada.math.mxparser.Function(noiseElement.Key, new FunctionNoise2D(noiseElement.Value.noise));
    //             elements[count] = f;
    //         }
    //         else if (noiseElement.Value.dimension == 3)
    //         {
    //             org.mariuszgromada.math.mxparser.Function f = new org.mariuszgromada.math.mxparser.Function(noiseElement.Key, new FunctionNoise3D(noiseElement.Value.noise));
    //             elements[count] = f;
    //         }
    //
    //         count++;
    //     }
    //
    //     e = new Expression(equation, elements);
    // }
    //
    // public double EquationResult(float x, float y, float z)
    // {
    //     e.setArgumentValue("x", x);
    //     e.setArgumentValue("y", y);
    //     e.setArgumentValue("z", z);
    //     
    //     double result = e.calculate();
    //     
    //     return result;
    // }
    #endregion
    
}

public struct EquationNoise
{
    public int dimension;
    public FastNoiseLite noise;
}
public static class CustomFunctions
{
    public static int Product(int a, int b)
    {
        return a * b;
    }

    public static int Sum(int a, int b)
    {
        return a + b;
    }
}
