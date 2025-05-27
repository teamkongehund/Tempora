using Godot;
using Tempora.Classes.Audio;

namespace Tempora.Classes.Visual.AudioDisplay;
internal interface IAudioSegmentDisplay
{
    public float Width { get; set; }
    public float Height { get; set; }
    public AudioFile AudioFile { get; set; }
    public float[] TimeRange { get; set; }
    public Color Color { get; set; }
    public bool Visible { get; set; }
    public void Render();
}
