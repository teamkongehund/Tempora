using Godot;
using System;

public partial class GridLine : Line2D
{
	public float RelativeMusicPosition;
	public int Divisor;
	public int DivisionIndex;
	public int[] TimeSignature;

	public GridLine(int[] timeSignature, int divisor, int index)
	{
		TimeSignature = timeSignature;
		Divisor = divisor;
		DivisionIndex = index;
        RelativeMusicPosition = Timing.GetRelativeNotePosition(timeSignature, divisor, index);

        DefaultColor = new Color(1f, 0, 0);
        Width = 1;
    }

	// TODO: Add a method that determines color. Execute it in _Ready().
}
