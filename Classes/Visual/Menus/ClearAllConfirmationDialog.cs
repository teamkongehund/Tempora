using Godot;
using System;
using Tempora.Classes.TimingClasses;

public partial class ClearAllConfirmationDialog : ConfirmationDialog
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        Confirmed += OnClearAllConfirmationDialogConfirmed;
    }

    private void OnClearAllConfirmationDialogConfirmed()
    {
        Timing.Instance.DeleteAllTimingPoints();
        Timing.Instance.DeleteAllTimeSignaturePoints();
    }
}
