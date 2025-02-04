using Godot;
using System;
using Tempora.Classes.TimingClasses;
using Tempora.Classes.Utility;
using Tempora.Classes.Visual;

public partial class TimeSignatureStepper : HBoxContainer
{
    [Export]
    public Stepper NumeratorStepper = null!;

    [Export]
    public Stepper DenominatorStepper = null!;

    private int[] timeSignature = [0, 0];

    public int[] TimeSignature
    {
        get => timeSignature;
        set
        {
            timeSignature = value;
            NumeratorStepper.DisplayedValue = timeSignature[0];
            DenominatorStepper.DisplayedValue = timeSignature[1];
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        NumeratorStepper.ValueModified += OnAnyStepperValueModified;
    }

    private void OnAnyStepperValueModified(object? sender, EventArgs e)
    {
        if (e is not GlobalEvents.ObjectArgument<int> intArgument)
            throw new ArgumentException($"Expected EventArgs argument to be of type {nameof(GlobalEvents.ObjectArgument<int>)}, " +
                $"corresponding with {nameof(Stepper.DisplayedValue)}");
        int[] newTimeSignature = timeSignature;
        if (sender == NumeratorStepper)
            newTimeSignature[0] = intArgument.Value;
        else if (sender == DenominatorStepper)
            newTimeSignature[1] = intArgument.Value;
    }
}
