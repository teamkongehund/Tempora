using System;
using Godot;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class TimeSignatureLineEdit : LineEdit
{
    public event EventHandler TimeSignatureSubmitted = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() => TextSubmitted += OnTextSubmitted;

    private void OnTextSubmitted(string text)
    {
        string[] textSplit = text.Split("/", 2);

        if (textSplit.Length != 2)
            return;

        bool upperIsInt = int.TryParse(textSplit[0], out int upper);
        bool lowerIsInt = int.TryParse(textSplit[1], out int lower);

        if (!upperIsInt || !lowerIsInt)
            return;

        if (upper < 1)
            return;

        int[] timeSignature = [upper, lower];

        ReleaseFocus();
        TimeSignatureSubmitted?.Invoke(this, new GlobalEvents.ObjectArgument<int[]>(timeSignature));
    }
}