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
        GlobalEvents.Instance.AudioFileChanged += OnAudioFileChanged;
        GlobalEvents.Instance.AudioVisualsContainerScrolled += OnScrolled;
        ValueChanged += OnValueChanged;
        UpdateScrollBar();
    }

    private void OnTimingChanged(object? sender, EventArgs e) => UpdateScrollBar();

    private void OnAudioFileChanged(object? sender, EventArgs e) => UpdateScrollBar();

    private void OnValueChanged(double value) => UpdateAudioVisualsContainer(value);

    private void UpdateScrollBar()
    {
        UpdateLimits();
        Value = audioVisualsContainer.NominalMusicPositionStartForTopBlock;
    }

    private void UpdateAudioVisualsContainer(double value)
    {
        if (isRangeChanging)
        {
            isRangeChanging = false;
            return; // If not in place, inadvertent scrolling occurs due to MinValue or MaxValue changing Value.
        }

        audioVisualsContainer.NominalMusicPositionStartForTopBlock = (int)value;
    }

    public void UpdateLimits()
    {
        //int firstMeasure = audioVisualsContainer.FirstTopMeasure;
        //int lastMeasure = audioVisualsContainer.LastTopMeasure;
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

    private void OnScrolled(object? sender, EventArgs e)
    {
        Value = audioVisualsContainer.NominalMusicPositionStartForTopBlock;
    }
}