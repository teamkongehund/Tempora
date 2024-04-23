using Godot;
using NAudio.SoundFont;
using System;
using System.Collections.Generic;
using Tempora.Classes.TimingClasses;
using Tempora.Classes.Utility;

namespace Tempora.Classes.Visual;

public partial class EditMenu : PopupMenu
{
    [Export]
    private ConfirmationDialog clearAllConfirmationDialog = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        IdPressed += OnIdPressed;
        clearAllConfirmationDialog.Confirmed += Timing.Instance.DeleteAllTimingPoints;
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
}
