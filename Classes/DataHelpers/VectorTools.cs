// Copyright 2024 https://github.com/kongehund
// 
// This file is licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0).
// You are free to:
// - Share — copy and redistribute the material in any medium or format
//
// Under the following terms:
// - Attribution — You must give appropriate credit, provide a link to the license, and indicate if changes were made.
// - NonCommercial — You may not use the material for commercial purposes.
// - NoDerivatives — If you remix, transform, or build upon the material, you may not distribute the modified material.
//
// Full license text is available at: https://creativecommons.org/licenses/by-nc-nd/4.0/legalcode

using System;
using Godot;

namespace Tempora.Classes.Utility;

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