using System;
using System.Linq;
using Godot;
using OsuTimer.Classes.Audio;
using OsuTimer.Classes.Utility;

// Tempora

namespace OsuTimer.Classes.Visual;

public partial class Main : Control
{
    //[Export]
    //private string audioPath = "res://Audio/UMO.mp3";
    [Export]
    private AudioStreamMP3 defaultMP3 = null!;
    [Export]
    private AudioPlayer audioPlayer = null!;
    [Export]
    private AudioVisualsContainer audioVisualsContainer = null!;
    [Export]
    private Metronome metronome = null!;
    [Export]
    private BlockScrollBar blockScrollBar = null!;
    [Export]
    private HScrollBar blockAmountScrollBar = null!;
    [Export]
    private HScrollBar gridScrollBar = null!;
    [Export]
    private HScrollBar offsetScrollBar = null!;
    [Export]
    private HScrollBar overlapScrollBar = null!;
    [Export]
    private HScrollBar playbackRateScrollBar = null!;
    [Export]
    private Label errorLabel = null!;

    //AudioFile AudioFile;

    private ProjectFileManager projectFileManager = null!;

    //private AudioDisplayPanel waveformWindow;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        projectFileManager = ProjectFileManager.Instance;

        // This works in Debug if we use i.e. audioPath = "res://Audio/UMO.mp3",
        // but won't work in production, as resources are converted to different file formats.
        //Project.Instance.AudioFile = new AudioFile(audioPath);

        Project.Instance.AudioFile = new AudioFile(defaultMP3);

        Signals.Instance.Scrolled += OnScrolled;
        Signals.Instance.SettingsChanged += OnSettingsChanged;
        audioVisualsContainer.SeekPlaybackTime += OnSeekPlaybackTime;
        audioVisualsContainer.AttemptToAddTimingPoint += OnAttemptToAddTimingPoint;
        //blockScrollBar.ValueChanged += OnBlockScrollBarValueChanged;
        GetTree().Root.FilesDropped += OnFilesDropped;
        //gridScrollBar.ValueChanged += OnGridScrollBarValueChanged;
        //playbackRateScrollBar.ValueChanged += OnPlaybackRateScrollBarValueChanged;
        //blockAmountScrollBar.ValueChanged += OnBlockAmountScrollBarValueChanged;
        //offsetScrollBar.ValueChanged += OnOffsetScrollBarValueChanged;
        //overlapScrollBar.ValueChanged += OnOverlapScrollBarValueChanged;

        audioVisualsContainer.CreateBlocks();
        UpdatePlayHeads();
        blockScrollBar.UpdateRange();
        audioVisualsContainer.UpdateBlocksScroll();
    }

    public override void _Input(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventKey keyEvent:
                {
                    if (keyEvent.Keycode == Key.Space && keyEvent.Pressed)
                        PlayPause();
                    break;
                }
            case InputEventMouseButton mouseEvent:
                {
                    //GD.Print("Main registered Input mouse event");
                    //GrabFocus();
                    //ReleaseFocus();
                    if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsReleased())
                    {
                        // Ensure a mouse release is always captured.
                        Signals.Instance.EmitEvent(Signals.Events.MouseLeftReleased);
                        Context.Instance.IsSelectedMusicPositionMoving = false;
                    }

                    break;
                }
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsPressed())
                GrabFocus();
            ReleaseFocus();
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (audioPlayer.Playing)
        {
            UpdatePlayHeads();
            UpdateMetronome();
        }
    }

    private void OnSettingsChanged(object? sender, EventArgs e) => audioVisualsContainer.UpdateNumberOfVisibleBlocks();

    private void OnFilesDropped(string[] filePaths)
    {
        if (filePaths.Length != 1)
            return;
        string path = filePaths[0];

        AudioFile audioFile;
        //try {
        //    audioFile = new AudioFile(path);
        //    Project.Instance.AudioFile = audioFile;
        //    audioPlayer.LoadMp3();
        //    //UpdateChildrensAudioFiles();
        //}
        //catch (Exception ex)
        //{
        //    errorLabel.Text = ex.Message;
        //}

        audioFile = new AudioFile(path);
        Project.Instance.AudioFile = audioFile;
        audioPlayer.LoadMp3();
    }

    //public void OnExportButtonPressed() {
    //    ExportOsz();

    //    //ExportOsu();

    //    exportButton.ReleaseFocus();
    //}

    //public void OnClearAllButtonPressed() {
    //    Timing.Instance.TimingPoints.Clear();
    //    Timing.Instance.TimeSignaturePoints.Clear();
    //    Signals.Instance.EmitEvent(Signals.Events.TimingChanged);;
    //}

    //private void OnBlockScrollBarValueChanged(double value) => audioVisualsContainer.NominalMusicPositionStartForTopBlock = (int)value;

    //private static void OnGridScrollBarValueChanged(double value)
    //{
    //    int intValue = (int)value;
    //    Settings.Instance.Divisor = Settings.SliderToDivisorDict[intValue];
    //}

    //private void OnPlaybackRateScrollBarValueChanged(double value) => audioPlayer.PitchScale = (float)value;

    //private void OnBlockAmountScrollBarValueChanged(double value) {
    //	var intValue = (int)value;
    //	Settings.Instance.NumberOfBlocks = intValue;
    //}

    //private void OnOffsetScrollBarValueChanged(double value) {
    //	Settings.Instance.MusicPositionOffset = (float)value;
    //}

    //private void OnOverlapScrollBarValueChanged(double value) {
    //	Settings.Instance.MusicPositionMargin = (float)value;
    //}

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

    public void Play() => audioPlayer.Play();

    public void Stop()
    {
        audioPlayer.Stop();
        UpdatePlayHeads();
    }

    public void PlayPause()
    {
        audioPlayer.PlayPause();
        UpdatePlayHeads();
    }

    public void OnScrolled(object? sender, EventArgs e)
    {
        UpdatePlayHeads();
        blockScrollBar.Value = audioVisualsContainer.NominalMusicPositionStartForTopBlock;
    }

    public void UpdatePlayHeads()
    {
        double playbackTime = audioPlayer.GetPlaybackTime();
        float musicPosition = Timing.Instance.TimeToMusicPosition((float)playbackTime);
        foreach (AudioBlock audioBlock in audioVisualsContainer.GetChildren().OfType<AudioBlock>())
        {
            AudioDisplayPanel audioDisplayPanel = audioBlock.AudioDisplayPanel;
            float x = audioDisplayPanel.MusicPositionToXPosition(musicPosition);
            audioDisplayPanel.Playhead.Position = new Vector2(x, 0.0f);
            audioDisplayPanel.Playhead.Visible = x >= 0 && x <= audioDisplayPanel.Size.X && audioPlayer.Playing;
        }
    }

    public void UpdateMetronome()
    {
        double playbackTime = audioPlayer.GetPlaybackTime();
        float musicPosition = Timing.Instance.TimeToMusicPosition((float)playbackTime);
        metronome.Click(musicPosition);
    }

    public void OnSeekPlaybackTime(float playbackTime) => audioPlayer.SeekPlay(playbackTime);//audioPlayer.SeekPlayHarsh(playbackTime);

    public static void OnAttemptToAddTimingPoint(float playbackTime)
    {
        Timing.Instance.AddTimingPoint(playbackTime, out TimingPoint? timingPoint);
        if (timingPoint == null)
            return;
        if (timingPoint.MusicPosition == null)
            throw new NullReferenceException($"{nameof(timingPoint.MusicPosition)} was null");
        Context.Instance.HeldTimingPoint = timingPoint;
        float musicPosition = (float)timingPoint.MusicPosition;
        Timing.SnapTimingPoint(timingPoint, musicPosition);
    }
}
