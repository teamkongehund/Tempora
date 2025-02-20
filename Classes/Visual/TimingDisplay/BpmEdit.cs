using Godot;
using System;
using Tempora.Classes.TimingClasses;
using Tempora.Classes.Utility;

public partial class BpmEdit : LineEdit
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        TextSubmitted += OnTextSubmitted;
        FocusExited += Cancel;
	}

    public void ShowAndEdit()
    {
        Text = "";
        Visible = true;
        GrabFocus();
    }

    private void OnTextSubmitted(string text)
    {
        SubmitBPM(text.ToFloat());
    }

    private void SubmitBPM(float bpm)
    {
        BpmSubmitted?.Invoke(this, EventArgs.Empty);
        Visible = false;
    }

    private void Cancel()
    {
        Visible = false;
        Canceled?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? BpmSubmitted = null;

    public event EventHandler? Canceled = null;
}
