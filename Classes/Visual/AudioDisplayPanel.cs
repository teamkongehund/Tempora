using Godot;
using System;
using OsuTimer.Classes.Utility;
using GD = OsuTimer.Classes.Utility.GD;

namespace OsuTimer.Classes.Visual;

/// <summary>
///     Parent class for window containing waveform(s), playhead and timing grid
/// </summary>
public partial class AudioDisplayPanel : Control {
    #region Properties & Signals

    [Signal]
    public delegate void SeekPlaybackTimeEventHandler(float playbackTime);

    [Signal]
    public delegate void AttemptToAddTimingPointEventHandler(float playbackTime);

    [Export]
    public Line2D Playhead;
    [Export]
    private Node2D waveformSegments;
    [Export]
    private Node2D VisualTimingPointFolder;
    [Export]
    private Node2D GridFolder;
    [Export]
    private PreviewLine PreviewLine;
    [Export]
    private Line2D SelectedPositionLine;
    //[Export]
    //private Label MeasureLabel;
    //[Export]
    //private TimeSignatureLineEdit TimeSignatureLineEdit;

    public event AttemptToAddTimingPointEventHandler SomeEvent;

    private PackedScene packedVisualTimingPoint = ResourceLoader.Load<PackedScene>("res://Classes/Visual/VisualTimingPoint.tscn");

    public bool IsInstantiating = true;

    private bool mouseIsInside = false;

    private int musicPositionStart;

    public int NominalMusicPositionStartForWindow {
        get => musicPositionStart;
        set {
            if (musicPositionStart == value) return;
            musicPositionStart = value;

            UpdateVisuals();
        }
    }

    public float ActualMusicPositionStartForWindow {
        get => NominalMusicPositionStartForWindow - Settings.Instance.MusicPositionMargin - Settings.Instance.MusicPositionOffset;
        private set { }
    }

    /// <summary>
    ///     List of horizontally stacked waveforms to display
    /// </summary>
    //public List<Waveform> Waveforms = new List<Waveform>();

    //public float StartTime = 0;
    //public float EndTime = 10;
    public int FirstTimingPointIndex;

    public int LastTimingPointIndex;

    #endregion

    #region Godot & Signals

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        Playhead.ZIndex = 100;

        UpdatePlayheadScaling();
        UpdatePreviewLineScaling();
        UpdateSelectedPositionScaling();
        UpdateSelectedPositionLine();

        CreateEmptyTimingPoints(8);

        Resized += OnResized;
        Signals.Instance.Connect("SettingsChanged", Callable.From(OnSettingsChanged));
        Signals.Instance.Connect("SelectedPositionChanged", Callable.From(OnSelectedPositionChanged));
        Signals.Instance.Connect("Scrolled", Callable.From(UpdateSelectedPositionLine));
        Signals.Instance.AudioFileChanged += OnAudioFileChanged;

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;

        // If I used recommended += syntax here,
        // disposed WaveformWindows will still react to this signal, causing exceptions.
        // This seems to be a bug with the += syntax when the signal transmitter is an autoload
        // See https://github.com/godotengine/godot/issues/70414 (haven't read this through)
        Signals.Instance.Connect("TimingChanged", Callable.From(OnTimingChanged));
    }

    // Note to self: This can be used instead of .Connect to fix the signal issue.
    //public override void _ExitTree()
    //{
    //	Signals.Instance.TimingChanged -= OnTimingChanged;
    //}

    public override void _GuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouseEvent:
                {
                    float x = mouseEvent.Position.X;
                    float musicPosition = XPositionToMusicPosition(x);
                    float time = Timing.Instance.MusicPositionToTime(musicPosition);
                    GD.Print($"WaveformWindow was clicked at playback time {time} seconds");

                    if (Input.IsKeyPressed(Key.Alt))
                    {
                        Context.Instance.IsSelectedMusicPositionMoving = true;
                        Context.Instance.SelectedMusicPosition = XPositionToMusicPosition(x);
                    }
                    else
                    {
                        x = mouseEvent.Position.X;
                        musicPosition = XPositionToMusicPosition(x);
                        if (Input.IsKeyPressed(Key.Shift))
                        {
                            musicPosition = Timing.SnapMusicPosition(musicPosition);
                        }
                        time = Timing.Instance.MusicPositionToTime(musicPosition);
                        EmitSignal(nameof(AttemptToAddTimingPoint), time);
                        GetViewport().SetInputAsHandled();
                    }

                    break;
                }
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed:true } mouseEvent:
                {
                    float x = mouseEvent.Position.X;
                    float musicPosition = XPositionToMusicPosition(x);
                    float time = Timing.Instance.MusicPositionToTime(musicPosition);
                    EmitSignal(nameof(SeekPlaybackTime), time);
                    break;
                }
            case InputEventMouseMotion mouseMotion:
                {
                    var mousePos = mouseMotion.Position;
                    //GD.Print(mousePos.ToString());
                    float musicPosition = XPositionToMusicPosition(mousePos.X);
                    if (Input.IsKeyPressed(Key.Shift))
                    {
                        musicPosition = Timing.SnapMusicPosition(musicPosition);
                    }
                    //float mouseRelativeMusicPosition = XPositionToRelativeMusicPosition(mousePos.X);
                    
                    // HeldTimingPoint
                    if (Input.IsKeyPressed(Key.Ctrl) && Context.Instance.HeldTimingPoint != null)
                    {                        
                        float xMovement = mouseMotion.Relative.X;
                        float secondsDifference = xMovement * 0.0002f;
                        Context.Instance.HeldTimingPoint.Time -= secondsDifference;
                    }
                    else
                    {
                        Timing.SnapTimingPoint(Context.Instance.HeldTimingPoint, musicPosition);
                    }

                    // PreviewLine
                    UpdatePreviewLinePosition();

                    // SelectedPosition
                    if (!Context.Instance.IsSelectedMusicPositionMoving) return;
                    Context.Instance.SelectedMusicPosition = XPositionToMusicPosition(mousePos.X);
                    break;
                }
        }
    }

    public override void _Input(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventKey { Keycode: Key.Shift } keyEvent:
                {
                    //if (!mouseIsInside) return;
                    ////var mousePos = GetViewport().GetMousePosition();
                    //var mousePos = GetLocalMousePosition();
                    //float musicPosition = XPositionToMusicPosition(mousePos.X);
                    //GD.Print("First musicPosition = " + musicPosition);
                    //if (keyEvent.Pressed)
                    //{
                    //    musicPosition = Timing.SnapMusicPosition(musicPosition);
                    //}
                    //GD.Print("Second musicPosition = " + musicPosition);
                    //UpdatePreviewLinePosition(musicPosition);
                    //GetViewport().SetInputAsHandled();

                    UpdatePreviewLinePosition();
                    break;
                }
        }
    }

    public void OnTimingChanged() {
        if (!Visible) return;
        UpdateTimingPointsIndices();
        CreateWaveforms();
        RenderTimingPoints();
        CreateGridLines();
    }

    public void OnAudioFileChanged() {
        CreateWaveforms();
    }

    public void OnResized() {
        //GD.Print("Resized!");
        UpdateVisuals();
    }

    public void OnSettingsChanged() {
        UpdateVisuals();
    }

    public void OnSelectedPositionChanged() {
        UpdateSelectedPositionLine();
    }

    public void OnMouseEntered() {
        PreviewLine.Visible = true;
        mouseIsInside = true;
    }

    public void OnMouseExited() {
        PreviewLine.Visible = false;
        mouseIsInside = false;
    }

    #endregion

    #region Render

    public void CreateWaveforms() {
        foreach (var child in waveformSegments.GetChildren())
            if (child is Waveform waveform)
                waveform.QueueFree();
            else if (child is Sprite2D sprite) sprite.QueueFree();

        float margin = Settings.Instance.MusicPositionMargin;

        float timeWhereWindowBegins = Timing.Instance.MusicPositionToTime(ActualMusicPositionStartForWindow);
        float timeWhereWindowEnds = Timing.Instance.MusicPositionToTime(ActualMusicPositionStartForWindow + 1 + 2 * margin);

        if (Timing.Instance.TimingPoints.Count == 0) {
            float startTime = timeWhereWindowBegins;
            float endTime = timeWhereWindowEnds;

            var waveform = new Waveform(Project.Instance.AudioFile, Size.X, Size.Y, new float[2] { startTime, endTime }) {
                Position = new Vector2(0, Size.Y / 2)
            };

            waveformSegments.AddChild(waveform);

            //GD.Print($"There were no timing points... Using MusicPositionStart {MusicPositionStartForWindow} to set start and end times.");

            return;
        }

        //GD.Print($"Window {MusicPositionStart}: timeWhereWindowBeginds = {timeWhereWindowBegins} , timeWhereWindowEnds = {timeWhereWindowEnds}");

        UpdateTimingPointsIndices();

        // Create each waveform segment
        for (int i = FirstTimingPointIndex; i <= LastTimingPointIndex; i++) {
            //GD.Print($"Now rendering waveform for index {i}");
            var timingPoint = Timing.Instance.TimingPoints[i];

            float waveSegmentStartTime = i == FirstTimingPointIndex
                ? timeWhereWindowBegins
                : timingPoint.Time;

            float waveSegmentEndTime = i == LastTimingPointIndex
                ? timeWhereWindowEnds
                : Timing.Instance.TimingPoints[i + 1].Time;

            //GD.Print($"Timing Points {FirstTimingPointIndex} - {LastTimingPointIndex}: Time {waveSegmentStartTime} - {waveSegmentEndTime}");

            float musicPositionStart = Timing.Instance.TimeToMusicPosition(waveSegmentStartTime);
            float musicPositionEnd = Timing.Instance.TimeToMusicPosition(waveSegmentEndTime);

            float length = Size.X * (musicPositionEnd - musicPositionStart) / (1f + 2 * margin);
            float xPosition = Size.X * (musicPositionStart - ActualMusicPositionStartForWindow) / (1f + 2 * margin);

            var waveform = new Waveform(Project.Instance.AudioFile, length, Size.Y, new float[2] { waveSegmentStartTime, waveSegmentEndTime }) {
                Position = new Vector2(xPosition, Size.Y / 2)
            };

            // Randomize color, so it's easy to see what's happening
            //Random random = new Random();
            //waveform.DefaultColor = new Color((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), 1);

            // Note: this one also takes some processing
            waveformSegments.AddChild(waveform);



            // Experimentation with offscreen viewports in order to render waveform as a separate image to be manipulated
            
        }
    }

    /// <summary>
    ///     Instantiate an amount of <see cref="VisualTimingPoint" /> nodes and adds as children.
    ///     These are then modified whenever the visuals need to change, instead of re-instantiating them.
    ///     This is significantly faster than creating them anew each time.
    /// </summary>
    public void CreateEmptyTimingPoints(int amount) {
        foreach (var child in VisualTimingPointFolder.GetChildren())
            if (child is VisualTimingPoint visualTimingPoint)
                visualTimingPoint.QueueFree();

        var dummyTimingPoint = new TimingPoint {
            Time = 0f,
            MusicPosition = 0f,
            MeasuresPerSecond = 120
        };

        for (var i = 0; i < amount; i++) {
            VisualTimingPoint visualTimingPoint;
            AddVisualTimingPoint(dummyTimingPoint, out visualTimingPoint);
        }
    }

    public void AddVisualTimingPoint(TimingPoint timingPoint, out VisualTimingPoint visualTimingPoint) {
        visualTimingPoint = packedVisualTimingPoint.Instantiate() as VisualTimingPoint;
        visualTimingPoint.Visible = false;
        visualTimingPoint.Scale = new Vector2(0.2f, 0.2f);
        visualTimingPoint.ZIndex = 95;
        visualTimingPoint.TimingPoint = timingPoint;
        VisualTimingPointFolder.AddChild(visualTimingPoint);
    }

    public void RenderTimingPoints() {
        var children = VisualTimingPointFolder.GetChildren();
        int childrenAmount = children.Count;
        var index = 0;

        foreach (Node2D child in children) child.Visible = false;

        // this assumes all children are VisualTimingPoint
        foreach (var timingPoint in Timing.Instance.TimingPoints) {
            if (timingPoint.MusicPosition < ActualMusicPositionStartForWindow
                || timingPoint.MusicPosition >= ActualMusicPositionStartForWindow + 1 + 2 * Settings.Instance.MusicPositionMargin)
                continue;

            VisualTimingPoint visualTimingPoint;
            if (index >= childrenAmount)
                AddVisualTimingPoint(timingPoint, out visualTimingPoint);
            else
                visualTimingPoint = children[index] as VisualTimingPoint;
            float x = MusicPositionToXPosition((float)timingPoint.MusicPosition);
            visualTimingPoint.TimingPoint = timingPoint;
            visualTimingPoint.Position = new Vector2(x, Size.Y / 2);
            visualTimingPoint.UpdateLabels(timingPoint);
            visualTimingPoint.Visible = true;
            index++;
        }
    }

    public void CreateGridLines() {
        foreach (var child in GridFolder.GetChildren()) child.QueueFree();

        int divisor = Settings.Instance.Divisor;
        int[] timeSignature = Timing.Instance.GetTimeSignature(NominalMusicPositionStartForWindow - 1);

        int measureOffset = -1;
        var divisionIndex = 0;
        while (divisionIndex < 50) {
        //using (
            var gridLine = GetGridLine(timeSignature, divisor, divisionIndex, measureOffset); //)
        //{
            divisionIndex++;

            float musicPosition = gridLine.RelativeMusicPosition + NominalMusicPositionStartForWindow + measureOffset;

            if (musicPosition >= NominalMusicPositionStartForWindow && measureOffset == -1) {
                divisionIndex = 0;
                measureOffset += 1;
                timeSignature = Timing.Instance.GetTimeSignature(NominalMusicPositionStartForWindow + measureOffset);
                continue;
            }

            if (musicPosition >= NominalMusicPositionStartForWindow + 1 && measureOffset == 0) {
                divisionIndex = 0;
                measureOffset += 1;
                timeSignature = Timing.Instance.GetTimeSignature(NominalMusicPositionStartForWindow + measureOffset);
                continue;
            }

            if (musicPosition < ActualMusicPositionStartForWindow)
                continue;
            if (musicPosition > ActualMusicPositionStartForWindow + 1 + 2 * Settings.Instance.MusicPositionMargin)
                break;

            timeSignature = Timing.Instance.GetTimeSignature(musicPosition);

            gridLine.ZIndex = 0;

            GridFolder.AddChild(gridLine);
        //}
        }
    }

    public GridLine GetGridLine(int[] timeSignature, int divisor, int index, int measureOffset) {
        var gridLine = new GridLine(timeSignature, divisor, index);

        float offset = Settings.Instance.MusicPositionOffset;
        float margin = Settings.Instance.MusicPositionMargin;
        float xPosition = Size.X * ((gridLine.RelativeMusicPosition + measureOffset + margin + offset) / (2 * margin + 1f));
        gridLine.Position = new Vector2(xPosition, 0);
        gridLine.Points = new Vector2[2] {
            new(0, 0),
            new(0, Size.Y)
        };
        gridLine.ZIndex = 90;

        return gridLine;
    }

    #endregion

    #region Updaters

    public void UpdateVisuals() {
        if (IsInstantiating) return;
        if (!Visible) return;
        UpdatePlayheadScaling();
        UpdatePreviewLineScaling();
        UpdatePreviewLinePosition();
        UpdateSelectedPositionScaling();
        CreateWaveforms(); // takes 2-5 ms on 30 blocks  loaded
        RenderTimingPoints(); // takes 2-3 ms on 30 blocks loaded
        CreateGridLines();
        
        // This works fine
        //await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        //GetViewport().GetTexture().GetImage().SavePng("user://renderedWave.png");
    }

    public void UpdatePlayheadScaling() {
        Playhead.Points = new Vector2[2] {
            new(0, 0),
            new(0, Size.Y)
        };
    }

    public void UpdatePreviewLineScaling() {
        PreviewLine.Points = new Vector2[2] {
            new(0, 0),
            new(0, Size.Y)
        };
    }

    //private void UpdatePreviewLinePosition(float musicPosition)
    //{
    //    PreviewLine.Position = new Vector2(MusicPositionToXPosition(musicPosition), 0);

    //    TimeSpan musicTime = TimeSpan.FromSeconds(Timing.Instance.MusicPositionToTime(musicPosition));
    //    PreviewLine.TimeLabel.Text = musicTime.ToString(@"mm\:ss\:fff");
    //}

    private void UpdatePreviewLinePosition()
    {
        var mousePos = GetLocalMousePosition();
        float musicPosition = XPositionToMusicPosition(mousePos.X);

        if (Input.IsKeyPressed(Key.Shift))
        {
            musicPosition = Timing.SnapMusicPosition(musicPosition);
        }

        PreviewLine.Position = new Vector2(MusicPositionToXPosition(musicPosition), 0);

        float time = Timing.Instance.MusicPositionToTime(musicPosition);
        TimeSpan musicTime = TimeSpan.FromSeconds(time);
        PreviewLine.TimeLabel.Text = (time < 0 ? "-" : "") + musicTime.ToString(@"mm\:ss\:fff");
    }

    public void UpdateSelectedPositionLine() {
        float x = MusicPositionToXPosition(Context.Instance.SelectedMusicPosition);
        SelectedPositionLine.Position = new Vector2(x, 0);
        //SelectedPositionLine.Visible = x >= 0 && x <= Size.X;
    }

    public void UpdateSelectedPositionScaling() {
        SelectedPositionLine.Points = new Vector2[2] {
            new(0, 0),
            new(0, Size.Y)
        };
    }

    public void UpdateTimingPointsIndices() {
        if (Timing.Instance.TimingPoints.Count == 0) return;

        float margin = Settings.Instance.MusicPositionMargin;

        int firstIndex = Timing.Instance.TimingPoints.FindLastIndex(point => point.MusicPosition <= ActualMusicPositionStartForWindow);
        // If there's only TimingPoints AFTER MusicPositionStart
        if (firstIndex == -1)
            firstIndex = Timing.Instance.TimingPoints.FindIndex(point => point.MusicPosition > ActualMusicPositionStartForWindow);
        int lastIndex = Timing.Instance.TimingPoints.FindLastIndex(point => point.MusicPosition < ActualMusicPositionStartForWindow + 1 + 2 * margin);
        if (lastIndex == -1) lastIndex = firstIndex;

        FirstTimingPointIndex = firstIndex;
        LastTimingPointIndex = lastIndex;
    }

    #endregion

    #region Calculators

    public float XPositionToMusicPosition(float x) {
        //GD.Print($"XPositionToMusicPosition: NominalMusicPositionStartForWindow = {NominalMusicPositionStartForWindow}");
        return XPositionToRelativeMusicPosition(x) + NominalMusicPositionStartForWindow;
    }

    /// <summary>
    ///     Return music position relative to <see cref="NominalMusicPositionStartForWindow" />
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public float XPositionToRelativeMusicPosition(float x) {
        float offset = Settings.Instance.MusicPositionOffset;
        float margin = Settings.Instance.MusicPositionMargin;
        float windowLengthInMeasures = 1f + 2 * margin;
        return x * windowLengthInMeasures / Size.X - margin - offset;
    }

    public float MusicPositionToXPosition(float musicPosition) {
        float offset = Settings.Instance.MusicPositionOffset;
        float margin = Settings.Instance.MusicPositionMargin;
        float windowLengthInMeasures = 1f + 2 * margin;
        return Size.X * (musicPosition - NominalMusicPositionStartForWindow + margin + offset) / windowLengthInMeasures;
    }

    #endregion
}