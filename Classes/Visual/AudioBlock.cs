using System;
using Godot;
using Tempora.Classes.Utility;
using Tempora.Classes.TimingClasses;

namespace Tempora.Classes.Visual;

public partial class AudioBlock : Control
{
    [Export]
    public AudioDisplayPanel AudioDisplayPanel = null!;
    [Export]
    private Label measureLabel = null!;
    [Export]
    private TimeSignatureLineEdit timeSignatureLineEdit = null!;

    private int musicPositionStart;
    public int NominalMusicPositionStartForWindow
    {
        get => musicPositionStart;
        set
        {
            musicPositionStart = value;

            AudioDisplayPanel.NominalMusicPositionStartForWindow = NominalMusicPositionStartForWindow;
            UpdateLabels();
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Signals.Instance.TimingChanged += OnTimingChanged;

        timeSignatureLineEdit.TimeSignatureSubmitted += OnTimingSignatureSubmitted;
    }

    private void OnTimingChanged(object? sender, EventArgs e)
    {
        if (!Visible)
            return;
        UpdateLabels();
    }

    private void OnTimingSignatureSubmitted(object? sender, EventArgs e)
    {
        if (e is not Signals.ObjectArgument<int[]> intArrayArgument)
            throw new Exception($"{nameof(e)} was not of type {nameof(Signals.ObjectArgument<int[]>)}");
        int[] timeSignature = intArrayArgument.Value;
        Timing.Instance.UpdateTimeSignature(timeSignature, NominalMusicPositionStartForWindow);
    }

    public void UpdateLabels()
    {
        int[] timeSignature = Timing.Instance.GetTimeSignature(NominalMusicPositionStartForWindow);
        measureLabel.Text = NominalMusicPositionStartForWindow.ToString();
        timeSignatureLineEdit.Text = $"{timeSignature[0]}/{timeSignature[1]}";
    }
}