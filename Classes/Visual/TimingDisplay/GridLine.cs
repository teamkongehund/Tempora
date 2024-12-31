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

using Godot;
using Tempora.Classes.TimingClasses;
using Color = Godot.Color;

namespace Tempora.Classes.Visual;

public partial class GridLine : Line2D
{
    public int DivisionIndex;
    public int Divisor;
    public int[] TimeSignature;
    public float RelativeMusicPosition;

    private float audioHeight;

    private float largeHeight = 0.5f;
    private float smallHeight = 0.2f;

    private Color color_Unspecified = new Color(0, 0, 0.7f, 1f);
    private Color color_Downbeat = new Color(1f, 0.2f, 0.2f, 1f);
    private Color color_16 = new Color(0.7f, 0, 0, 1f);
    private Color color_12 = new Color("7572ff");

    public GridLine(int[] timeSignature, int divisor, int index, float audioHeight)
    {
        TimeSignature = timeSignature;
        Divisor = divisor;
        DivisionIndex = index;
        this.audioHeight = audioHeight;
        RelativeMusicPosition = Timing.GetRelativeNotePosition(timeSignature, divisor, index);
        //new ColorConverter();

        DefaultColor = color_Unspecified;
        //DefaultColor = (Godot.Color) converter.ConvertFromString("#FFDFD991");

        Width = 5;

        UpdateColor();
        UpdatePoints();
    }

    private void UpdateColor()
    {
        bool isOnDownbeat = RelativeMusicPosition == 0;
        bool isOn16 = Timing.IsDivisionOnDivisor(Divisor, DivisionIndex, 16);
        bool isOn12 = Timing.IsDivisionOnDivisor(Divisor, DivisionIndex, 12);

        DefaultColor = (isOnDownbeat ? color_Downbeat : isOn16 ? color_16 : isOn12 ? color_12 : color_Unspecified);
    }

    private void UpdatePoints()
    {
        float height = GetHeight();
        Points = [
            new(0, -height / 2 * audioHeight),
            new(0, height / 2 * audioHeight)
        ];
    }

    private float GetHeight()
    {
        bool isOnQuarterNote = Timing.IsDivisionOnDivisor(Divisor, DivisionIndex, 4);

        return (isOnQuarterNote ? largeHeight : smallHeight);
    }
}