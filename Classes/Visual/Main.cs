using System;
using System.Linq;
using Godot;
using OsuTimer.Classes.Audio;
using OsuTimer.Classes.Utility;

// Tempora

namespace OsuTimer.Classes.Visual;

public partial class Main : Control {
    [Export]
    private string audioPath = "res://Audio/click.mp3";
    [Export] 
    private AudioPlayer audioPlayer;
    [Export]
    private AudioVisualsContainer audioVisualsContainer;
    [Export] 
    private Metronome metronome;
    [Export]
    private BlockScrollBar blockScrollBar;
    [Export]
    private HScrollBar blockAmountScrollBar;
    [Export]
    private HScrollBar gridScrollBar;
    [Export]
    private HScrollBar offsetScrollBar;
    [Export]
    private HScrollBar overlapScrollBar;
    [Export]
    private HScrollBar playbackRateScrollBar;


    //AudioFile AudioFile;

    private ProjectFileManager projectFileManager;

    private WaveformWindow waveformWindow;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        projectFileManager = ProjectFileManager.Instance;
        Project.Instance.AudioFile = new AudioFile(audioPath);

        Signals.Instance.Scrolled += OnScrolled;
        Signals.Instance.SettingsChanged += OnSettingsChanged;
        audioVisualsContainer.SeekPlaybackTime += OnSeekPlaybackTime;
        audioVisualsContainer.DoubleClicked += OnDoubleClick;
        blockScrollBar.ValueChanged += OnBlockScrollBarValueChanged;
        GetTree().Root.FilesDropped += OnFilesDropped;
        gridScrollBar.ValueChanged += OnGridScrollBarValueChanged;
        playbackRateScrollBar.ValueChanged += OnPlaybackRateScrollBarValueChanged;
        blockAmountScrollBar.ValueChanged += OnBlockAmountScrollBarValueChanged;
        offsetScrollBar.ValueChanged += OnOffsetScrollBarValueChanged;
        overlapScrollBar.ValueChanged += OnOverlapScrollBarValueChanged;

        audioVisualsContainer.CreateBlocks();
        UpdatePlayHeads();
        blockScrollBar.UpdateRange();
    }

    public override void _Input(InputEvent inputEvent) {
        switch (inputEvent) {
            case InputEventKey keyEvent: {
                if (keyEvent.Keycode == Key.Space && keyEvent.Pressed)
                    PlayPause();

                break;
            }
            case InputEventMouseButton mouseEvent: {
                //GD.Print("Main registered Input mouse event");
                //GrabFocus();
                //ReleaseFocus();
                if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsReleased()) {
                    // Ensure a mouse release is always captured.
                    //GD.Print("Main: MouseLeftReleased");
                    Signals.Instance.EmitSignal("MouseLeftReleased");
                    Context.Instance.IsSelectedMusicPositionMoving = false;
                }

                break;
            }
        }
    }

    public override void _GuiInput(InputEvent @event) {
        if (@event is InputEventMouseButton mouseEvent) {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsPressed())
                GrabFocus();
            ReleaseFocus();
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
        if (audioPlayer.Playing) {
            UpdatePlayHeads();
            UpdateMetronome();
        }
    }

    private void OnSettingsChanged() {
        audioVisualsContainer.UpdateNumberOfBlocks();
    }

    private void OnFilesDropped(string[] filePaths) {
        if (filePaths.Length != 1) return;
        string path = filePaths[0];

        AudioFile audioFile;
        try {
            audioFile = new AudioFile(path);
            Project.Instance.AudioFile = audioFile;
            audioPlayer.LoadMp3();
            //UpdateChildrensAudioFiles();
        }
        catch { }
    }

    //public void OnExportButtonPressed() {
    //    ExportOsz();

    //    //ExportOsu();

    //    exportButton.ReleaseFocus();
    //}

    //public void OnClearAllButtonPressed() {
    //    Timing.Instance.TimingPoints.Clear();
    //    Timing.Instance.TimeSignaturePoints.Clear();
    //    Signals.Instance.EmitSignal("TimingChanged");
    //}

    private void OnBlockScrollBarValueChanged(double value) {
        audioVisualsContainer.NominalMusicPositionStartForTopBlock = (int)value;
    }

    private void OnGridScrollBarValueChanged(double value) {
        var intValue = (int)value;
        Settings.Instance.Divisor = Settings.SliderToDivisorDict[intValue];
    }

    private void OnPlaybackRateScrollBarValueChanged(double value) {
        audioPlayer.PitchScale = (float)value;
    }

    private void OnBlockAmountScrollBarValueChanged(double value) {
        var intValue = (int)value;
        Settings.Instance.NumberOfBlocks = intValue;
    }

    private void OnOffsetScrollBarValueChanged(double value) {
        Settings.Instance.MusicPositionOffset = (float)value;
    }

    private void OnOverlapScrollBarValueChanged(double value) {
        Settings.Instance.MusicPositionMargin = (float)value;
    }

    //public void ExportOsz() {
    //    var random = new Random();
    //    int rand = random.Next();
    //    var path = $"user://{rand}.osz";
    //    string dotOsu = OsuExporter.GetDotOsu(Timing.Instance);
    //    OsuExporter.SaveOsz(path, dotOsu, Project.Instance.AudioFile);

    //    // Open with system:
    //    string globalPath = ProjectSettings.GlobalizePath(path);
    //    if (FileAccess.FileExists(globalPath)) OS.ShellOpen(globalPath);
    //}

    public void Play() {
        audioPlayer.Play();
    }

    public void Stop() {
        audioPlayer.Stop();
        UpdatePlayHeads();
    }

    public void PlayPause() {
        audioPlayer.PlayPause();
        UpdatePlayHeads();
    }

    public void OnScrolled() {
        UpdatePlayHeads();
        blockScrollBar.Value = audioVisualsContainer.NominalMusicPositionStartForTopBlock;
    }

    public void UpdatePlayHeads() {
        double playbackTime = audioPlayer.GetPlaybackTime();
        float musicPosition = Timing.Instance.TimeToMusicPosition((float)playbackTime);
        foreach (var tempWaveformWindow in audioVisualsContainer.GetChildren().OfType<WaveformWindow>()) {
            float x = tempWaveformWindow.MusicPositionToXPosition(musicPosition);
            tempWaveformWindow.Playhead.Position = new Vector2(x, 0.0f);
            tempWaveformWindow.Playhead.Visible = x >= 0 && x <= tempWaveformWindow.Size.X && audioPlayer.Playing;
        }
    }

    public void UpdateMetronome() {
        double playbackTime = audioPlayer.GetPlaybackTime();
        float musicPosition = Timing.Instance.TimeToMusicPosition((float)playbackTime);
        metronome.Click(musicPosition);
    }

    public void OnSeekPlaybackTime(float playbackTime) {
        audioPlayer.SeekPlay(playbackTime);
        //audioPlayer.SeekPlayHarsh(playbackTime);
    }

    public void OnDoubleClick(float playbackTime) {
        TimingPoint timingPoint;
        Timing.Instance.AddTimingPoint(playbackTime, out timingPoint);
        if (timingPoint != null) {
            Context.Instance.HeldTimingPoint = timingPoint;
            Timing.SnapTimingPoint(timingPoint, (float)timingPoint.MusicPosition);
        }
    }
}