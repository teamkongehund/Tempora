using System.Drawing;
using Godot;
using OsuTimer.Classes.Utility;
using Color = Godot.Color;

namespace OsuTimer.Classes.Visual;

public partial class GridLine : Line2D {
    public int DivisionIndex;
    public int Divisor;
    public float RelativeMusicPosition;
    public int[] TimeSignature;

    public GridLine(int[] timeSignature, int divisor, int index) {
        TimeSignature = timeSignature;
        Divisor = divisor;
        DivisionIndex = index;
        RelativeMusicPosition = Timing.GetRelativeNotePosition(timeSignature, divisor, index);

        var converter = new ColorConverter();

        DefaultColor = new Color(0.7f, 0, 0, 0.7f);
        //DefaultColor = (Godot.Color) converter.ConvertFromString("#FFDFD991");

        Width = 3;

        UpdateColor();
    }

    public void UpdateColor() {
        if (RelativeMusicPosition == 0) {
            DefaultColor = new Color(1f, 0, 0, 0.7f);
            //DefaultColor = new Godot.Color(960000);
            new Color();
        }
    }
}