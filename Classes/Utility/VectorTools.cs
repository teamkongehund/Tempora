using System;
using Godot;

namespace OsuTimer.Classes.Utility;

public partial class VectorTools : Node
{
    public static Vector2[] CombineArraysToVector2(float[] xArray, float[] yArray)
    {
        int length = xArray.Length;
        var combinedArray = new Vector2[length];

        for (int i = 0; i < length; i++)
            combinedArray[i] = new Vector2(xArray[i], yArray[i]);

        return combinedArray;
    }

    public static float[] CreateLinearSpace(float minValue, float maxValue, int numberOfValues)
    {
        float[] result = [];
        try
        {
            result = new float[numberOfValues];
        }
        catch (Exception)
        {
            //
        }

        float step = (maxValue - minValue) / (numberOfValues - 1);

        for (int i = 0; i < numberOfValues; i++)
            result[i] = minValue + (step * i);

        return result;
    }
}