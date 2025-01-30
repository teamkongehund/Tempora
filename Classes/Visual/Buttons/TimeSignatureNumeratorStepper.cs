using Godot;
using System;
using Tempora.Classes.TimingClasses;
using Tempora.Classes.Utility;
using Tempora.Classes.Visual;

public partial class TimeSignatureNumeratorStepper : Stepper
{
    [Export]
    AudioBlock audioBlock = null!;

    [Export]
    Stepper timeSignatureDenominatorStepper = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        value = 4;
        base._Ready();
        GlobalEvents.Instance.AudioVisualsContainerScrolled += OnScrolled;
    }

    protected override void OnDecrementButtonPressed()
    {
        if (value == 1)
            return;
        UpdateValueAndTarget(value - 1);
    }

    protected override void UpdateTarget()
    {
        Timing.Instance.UpdateTimeSignature([value, timeSignatureDenominatorStepper.value], audioBlock.NominalMeasurePosition);
    }

    private void OnScrolled(object? sender, EventArgs e)
    {
        UpdateValue(Timing.Instance.GetTimeSignature(audioBlock.NominalMeasurePosition)[1]);
    }
}
