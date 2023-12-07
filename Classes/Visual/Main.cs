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

    // TODO 3: Add input field and/or number visualizer for dB on volume sliders

    // TODO 2: Add transient snapping:
    // A method finds the local loudest part of the song
    // Holding down a certain key combination and moving your mouse down through the transients will snap all of them when you release
    // So, whichever grid line you're closest to, all of them will snap to it - the grid line in question should light up.

    // TODO 2: Add offsetting option to timing points when you change time signature (keep measures or keep beats)

    // TODO 2: Add playhead in scrollbar

    // TODO 3: Update exisiting .osu files to synchronize to a specific beatmap folder.

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

    // TODO 2: Scroll to set BPM

    // TODO 2: Double / halve BPM for a point

    // TODO 2: Copy osu time stamp into app

    // TODO 2: Copy pasting groups of timing points

    // TODO 3: Detached reset points (points that work as metronome resets and don't force anything unto the previous.)

    // TODO 2: Spectral view (blackmann-harris rendering with 4096 bands or 2048 if it's too performance impacting)

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