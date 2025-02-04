using Godot;
using System;
using Tempora.Classes.TimingClasses;
using Tempora.Classes.Utility;
using Tempora.Classes.Visual;

public partial class TimeSignatureNumeratorStepper : Stepper
{
    [Export]
    Stepper timeSignatureDenominatorStepper = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        DisplayedValue = 4;
        base._Ready();
    }

    protected override void OnDecrementButtonPressed()
    {
        if (DisplayedValue == 1)
            return;
        ModifyValue(DisplayedValue - 1);
    }

    //protected override void UpdateTarget()
    //{
    //    Timing.Instance.UpdateTimeSignature([DisplayedValue, timeSignatureDenominatorStepper.DisplayedValue], audioBlock.NominalMeasurePosition);
    //}
}
