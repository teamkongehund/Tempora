using Godot;
using Tempora.Classes.TimingClasses;
using Color = Godot.Color;

namespace Tempora.Classes.Visual;

public partial class GridLine : Line2D
{
    public int DivisionIndex;
    public int Divisor;
    public float RelativeMusicPosition;
    public int[] TimeSignature;

    public GridLine(int[] timeSignature, int divisor, int index)
    {
        TimeSignature = timeSignature;
        Divisor = divisor;
        DivisionIndex = index;
        RelativeMusicPosition = Timing.GetRelativeNotePosition(timeSignature, divisor, index);
        //new ColorConverter();

        DefaultColor = new Color(0.7f, 0, 0, 1f);
        //DefaultColor = (Godot.Color) converter.ConvertFromString("#FFDFD991");

        Width = 5;

        UpdateColor();
    }

    public void UpdateColor()
    {
        if (RelativeMusicPosition == 0)
        {
            DefaultColor = new Color(1f, 0, 0, 1f);
        }
    }
}