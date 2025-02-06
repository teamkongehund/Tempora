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

    public event EventHandler? TimeSignatureSubmitted = null;

    private int[] denominatorValues = [1, 2, 4, 8, 16, 32, 64];

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
        NumeratorStepper.HandleChangesElsewhere = true;
        DenominatorStepper.HandleChangesElsewhere = true;
        NumeratorStepper.ValueIncremented += OnNumeratorIncremented;
        NumeratorStepper.ValueDecremented += OnNumeratorDecremented;
        DenominatorStepper.ValueIncremented += OnDenominatorIncremented;
        DenominatorStepper.ValueDecremented += OnDenominatorDecremented;
        NumeratorStepper.MouseExitedStepper += OnMouseExitedStepper;
    }

    public void UpdateLabelColor(Color color)
    {
        NumeratorStepper.ValueLabel.AddThemeColorOverride("font_color", color);
        DenominatorStepper.ValueLabel.AddThemeColorOverride("font_color", color);
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
        Timing.CorrectTimeSignature(newTimeSignature, out newTimeSignature); // Also done by Timing.Instance, may be redundant
        TimeSignatureSubmitted?.Invoke(this, new GlobalEvents.ObjectArgument<int[]>(newTimeSignature));
    }

    private void OnNumeratorIncremented(object? sender, EventArgs e)
    {
        TimeSignature = [NumeratorStepper.DisplayedValue + 1, timeSignature[1]];
    }

    private void OnNumeratorDecremented(object? sender, EventArgs e)
    {
        TimeSignature = [NumeratorStepper.DisplayedValue >= 1 ? NumeratorStepper.DisplayedValue - 1 : 1, timeSignature[1]];
    }

    private void OnDenominatorIncremented(object? sender, EventArgs e)
    {
        int denominator = DenominatorStepper.DisplayedValue;
        int index = Array.IndexOf(denominatorValues, denominator);
        if (index == -1 || index == denominatorValues.Length - 1)
            return;
        TimeSignature = [timeSignature[0], denominatorValues[index + 1]];
    }

    private void OnDenominatorDecremented(object? sender, EventArgs e)
    {
        int denominator = DenominatorStepper.DisplayedValue;
        int index = Array.IndexOf(denominatorValues, denominator);
        if (index == -1 || index == 1)
            return;
        TimeSignature = [timeSignature[0], denominatorValues[index - 1]];
    }

    private void SubmitTimeSignature() => TimeSignatureSubmitted?.Invoke(this, new GlobalEvents.ObjectArgument<int[]>(timeSignature));

    private void OnMouseExitedStepper(object? sender, EventArgs e)
    {
        SubmitTimeSignature();
    }
}
