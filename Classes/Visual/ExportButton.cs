using Godot;
using System;
using OsuTimer.Classes.Utility;

public partial class ExportButton : Button
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Pressed += OnPressed;
	}

	private void OnPressed()
	{
        OsuExporter.ExportOsz();
		ReleaseFocus();
    }
}
