using Godot;
using System;
using Tempora.Classes.TimingClasses;
using Tempora.Classes.Utility;
using Tempora.Classes.Visual;

public partial class TimeSignatureDenominatorStepper : Stepper
{
    [Export]
    AudioBlock audioBlock = null!;

    [Export]
    Stepper timeSignatureNumeratorStepper = null!;

    private int[] allowedValues = [1, 2, 4, 8, 16, 32, 64];

    private int currentValueIndex = 2;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        value = allowedValues[currentValueIndex];
        base._Ready();

        GlobalEvents.Instance.AudioVisualsContainerScrolled += OnScrolled;
    }

    protected override void OnIncrementButtonPressed()
    {
        currentValueIndex = currentValueIndex < allowedValues.Length - 1 ? currentValueIndex + 1 : currentValueIndex;
        UpdateValueAndTarget(allowedValues[currentValueIndex]);
    }

    protected override void OnDecrementButtonPressed()
    {
        currentValueIndex = currentValueIndex > 0 ? currentValueIndex - 1 : currentValueIndex;
        UpdateValueAndTarget(allowedValues[currentValueIndex]);
    }

    protected override void UpdateTarget()
    {
        Timing.Instance.UpdateTimeSignature([timeSignatureNumeratorStepper.value, value], audioBlock.NominalMeasurePosition);
    }

    private void OnScrolled(object? sender, EventArgs e)
    {
        UpdateValue(Timing.Instance.GetTimeSignature(audioBlock.NominalMeasurePosition)[1]);
    }
}
