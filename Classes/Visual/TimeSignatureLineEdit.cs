using Godot;
using System;

public partial class TimeSignatureLineEdit : LineEdit
{
	[Signal] public delegate void TimeSignatureSubmittedEventHandler(int[] timeSignature);

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		TextSubmitted += OnTextSubmitted;
	}

	public void OnTextSubmitted(string text)
	{
		string[] textSplit = text.Split("/", 2);

		if (textSplit.Length != 2)
			return;

		int upper;
		int lower;
		bool upperIsInt = int.TryParse(textSplit[0], out upper);
		bool lowerIsInt = int.TryParse(textSplit[1], out lower);

		if (!upperIsInt || !lowerIsInt)
			return;

		if (lower != 4 && lower != 8)
			return;

		if (upper < 1)
			return;

		int[] timeSignature = new int[] { upper, lower };

		ReleaseFocus();
        EmitSignal(nameof(TimeSignatureSubmitted), timeSignature);
	}
}
