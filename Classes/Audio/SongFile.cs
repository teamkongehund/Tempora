using Godot;
using System;
using System.Linq;

namespace Tempora.Classes.Audio;

public partial class SongFile : AudioFile
{
    public SongFile(string path) : base(path)
    {
    }

    public SongFile(AudioStreamMP3 audioStreamMP3) : base(audioStreamMP3)
    {
    }

    public override float[] AudioData
    {
        get => _audioData;
        set
        {
            _audioData = value;
            CalculatePer10s();
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public void CalculatePer10s()
    {
        int smallLength = (int)(AudioData.Length / 10f);
        bool isDataLengthDivisibleBy10 = AudioData.Length % 10 == 0;
        int length = isDataLengthDivisibleBy10 ? smallLength : smallLength + 1;

        AudioDataPer10Min = new float[length];
        AudioDataPer10Max = new float[length];

        for (int i = 0; i < length - 1; i++)
        {
            AudioDataPer10Min[i] = AudioData[(i * 10)..((i * 10) + 10)].Min();
            AudioDataPer10Max[i] = AudioData[(i * 10)..((i * 10) + 10)].Max();
        }
        AudioDataPer10Min[length - 1] = AudioData[((length - 1) * 10)..^1].Min();
        AudioDataPer10Max[length - 1] = AudioData[((length - 1) * 10)..^1].Max();
    }
}
