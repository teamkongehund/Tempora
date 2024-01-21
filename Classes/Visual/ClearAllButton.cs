using Godot;
using OsuTimer.Classes.Utility;

namespace OsuTimer.Classes.Visual;

public partial class ClearAllButton : Button
{
    [Export]
    private ConfirmationDialog confirmationDialog = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Pressed += OnPressed;
        confirmationDialog.Confirmed += ClearAllTimingPoints;
    }

    private void OnPressed() => confirmationDialog.Popup();

    private void ClearAllTimingPoints()
    {
        Timing.Instance.TimingPoints.Clear();
        Timing.Instance.TimeSignaturePoints.Clear();
        Signals.Instance.EmitEvent(Signals.Events.TimingChanged);;
        ReleaseFocus();

        ActionsHandler.Instance.AddTimingMemento();
    }
}
