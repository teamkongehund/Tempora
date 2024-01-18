using Godot;
using OsuTimer.Classes.Utility;
using OsuTimer.Classes.Visual;

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
        // If I used recommended += syntax here,
        // disposed WaveformWindows will still react to this signal, causing exceptions.
        // This seems to be a bug with the += syntax when the signal transmitter is an autoload
        // See https://github.com/godotengine/godot/issues/70414 (haven't read this through)
        Signals.Instance.Connect("TimingChanged", Callable.From(OnTimingChanged));

        timeSignatureLineEdit.Connect("TimeSignatureSubmitted", new Callable(this, "OnTimingSignatureSubmitted"));
    }

    public void OnTimingChanged()
    {
        if (!Visible) return;
        UpdateLabels();
    }

    public void OnTimingSignatureSubmitted(int[] timeSignature)
    {
        Timing.Instance.UpdateTimeSignature(timeSignature, NominalMusicPositionStartForWindow);
    }

    public void UpdateLabels()
    {
        int[] timeSignature = Timing.Instance.GetTimeSignature(NominalMusicPositionStartForWindow);
        measureLabel.Text = NominalMusicPositionStartForWindow.ToString();
        timeSignatureLineEdit.Text = $"{timeSignature[0]}/{timeSignature[1]}";
    }


}
