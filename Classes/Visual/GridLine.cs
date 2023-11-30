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

        DefaultColor = new Color(0.7f, 0, 0, 0.7f);
        Width = 3;

		UpdateColor();
    }

	// TODO 2: Add a method that determines color. Execute it in _Ready().
	public void UpdateColor()
	{
		if (RelativeMusicPosition == 0)
		{
			DefaultColor = new Color(1f, 0, 0, 0.7f);
        }
	}
}
