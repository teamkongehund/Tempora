using Godot;

namespace OsuTimer.Classes.Visual;

public partial class TimeSignatureLineEdit : LineEdit
{
    [Signal]
    public delegate void TimeSignatureSubmittedEventHandler(int[] timeSignature);

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() => TextSubmitted += OnTextSubmitted;

    public void OnTextSubmitted(string text)
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
        _ = EmitSignal(nameof(TimeSignatureSubmitted), timeSignature);
    }
}