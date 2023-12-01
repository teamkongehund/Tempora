using Godot;
using System;
using System.Drawing;

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

		ColorConverter converter = new ColorConverter();

        DefaultColor = new Godot.Color(0.7f, 0, 0, 0.7f);
		//DefaultColor = (Godot.Color) converter.ConvertFromString("#FFDFD991");

        Width = 3;

		UpdateColor();
    }

	// TODO 2: Add a method that determines color. Execute it in _Ready().
	public void UpdateColor()
	{
		if (RelativeMusicPosition == 0)
		{
            DefaultColor = new Godot.Color(1f, 0, 0, 0.7f);
            //DefaultColor = new Godot.Color(960000);
            new Godot.Color();
        }
	}
}
