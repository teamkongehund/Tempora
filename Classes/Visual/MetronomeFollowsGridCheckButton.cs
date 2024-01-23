using Godot;
using System;
using Tempora.Classes.Utility;

public partial class MetronomeFollowsGridCheckButton : CheckButton
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        Toggled += OnToggled;
	}

    private void OnToggled(bool state)
    {
        Settings.Instance.MetronomeFollowsGrid = state;
        ReleaseFocus();
    }
}
