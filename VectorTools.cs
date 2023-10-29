using Godot;
using System;

public partial class VectorTools : Node
{
    public static Vector2[] CombineArraysToVector2(float[] xArray, float[] yArray)
    {
        int length = xArray.Length;
        Vector2[] combinedArray = new Vector2[length];

        for (int i = 0; i < length; i++)
        {
            combinedArray[i] = new Vector2(xArray[i], yArray[i]);
        }

        return combinedArray;
    }

    public static float[] CreateLinearSpace(float minValue, float maxValue, int numberOfValues)
    {
        float[] result = new float[numberOfValues];
        float step = (maxValue - minValue) / (numberOfValues - 1);

        for (int i = 0; i < numberOfValues; i++)
        {
            result[i] = minValue + step * i;
        }

        return result;
    }
}
