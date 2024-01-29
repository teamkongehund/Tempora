using Godot;
using NAudio.SoundFont;
using System;
using System.Collections.Generic;
using Tempora.Classes.TimingClasses;
using Tempora.Classes.Utility;

public partial class EditMenu : PopupMenu
{
    [Export]
    private ConfirmationDialog clearAllConfirmationDialog = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        IdPressed += OnIdPressed;
        clearAllConfirmationDialog.Confirmed += ClearAllTimingPoints;
    }

    private void OnIdPressed(long id)
    {
        switch (id)
        {
            case 0:
                clearAllConfirmationDialog.Popup();
                break;
        }
    }

    private void OnPressed() => clearAllConfirmationDialog.Popup();

    private void ClearAllTimingPoints()
    {
        Timing.Instance.TimingPoints.Clear();
        Timing.Instance.TimeSignaturePoints.Clear();
        Signals.Instance.EmitEvent(Signals.Events.TimingChanged);

        ActionsHandler.Instance.AddTimingMemento();
    }
}
