using System;
using System.Reflection.Metadata;
using Godot;
using OsuTimer.Classes.Utility;

namespace OsuTimer.Classes.Visual;

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

        timeSignatureLineEdit.Connect("TimeSignatureSubmitted", new Callable(this, "OnTimingSignatureSubmitted"));
    }

    public void OnTimingChanged(object? sender, EventArgs e)
    {
        if (!Visible)
            return;
        UpdateLabels();
    }

    public void OnTimingSignatureSubmitted(int[] timeSignature) => Timing.Instance.UpdateTimeSignature(timeSignature, NominalMusicPositionStartForWindow);

    public void UpdateLabels()
    {
        int[] timeSignature = Timing.Instance.GetTimeSignature(NominalMusicPositionStartForWindow);
        measureLabel.Text = NominalMusicPositionStartForWindow.ToString();
        timeSignatureLineEdit.Text = $"{timeSignature[0]}/{timeSignature[1]}";
    }
}
