using System;
using Godot;
using Tempora.Classes.Utility;
using Tempora.Classes.TimingClasses;

namespace Tempora.Classes.Visual;

public partial class BlockScrollBar : VScrollBar
{
    [Export]
    AudioVisualsContainer audioVisualsContainer = null!;

    bool isRangeChanging = false;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GlobalEvents.Instance.TimingChanged += OnTimingChanged;
        ValueChanged += OnValueChanged;
    }

    private void OnTimingChanged(object? sender, EventArgs e)
    {
        UpdateRange();
        Value = audioVisualsContainer.NominalMusicPositionStartForTopBlock;
    }

    private void OnValueChanged(double value) => UpdateTopMeasure(value);

    private void UpdateTopMeasure(double value)
    {
        if (isRangeChanging)
        {
            isRangeChanging = false;
            return; // If not in place, inadvertent scrolling occurs due to MinValue or MaxValue changing Value.
        }

        audioVisualsContainer.NominalMusicPositionStartForTopBlock = (int)value;
    }

    public void UpdateRange()
    {
        int firstMeasure = (int)Timing.Instance.SampleTimeToMusicPosition(0);
        int lastMeasure = Timing.Instance.GetLastMeasure() - (Settings.Instance.NumberOfBlocks - 1);
        if (MinValue != firstMeasure)
        {
            isRangeChanging = true;
            MinValue = firstMeasure;
        }
        if (MaxValue != lastMeasure)
        {
            isRangeChanging = true;
            MaxValue = lastMeasure;
        }

        //GD.Print($"UpdateRange() result: Range: {MinValue}...{MaxValue} with Value = {Value}");
    }
}