// Copyright 2024 https://github.com/kongehund
// 
// This file is licensed under the Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International (CC BY-NC-ND 4.0).
// You are free to:
// - Share, copy and redistribute the material in any medium or format
//
// Under the following terms:
// - Attribution - You must give appropriate credit, provide a link to the license, and indicate if changes were made.
// - NonCommercial - You may not use the material for commercial purposes.
// - NoDerivatives - If you remix, transform, or build upon the material, you may not distribute the modified material.
//
// Full license text is available at: https://creativecommons.org/licenses/by-nc-nd/4.0/legalcode

using System;
using Godot;
using Tempora.Classes.Utility;

namespace Tempora.Classes.TimingClasses;

/// <summary>
/// A data class which asserts that a specific point in time (<see cref="Offset"/>)
/// should be attached to a musical timeline at (<see cref="MusicPosition"/>).
/// The Bpm (<see cref="Bpm"/>) is calculated via the subsequent <see cref="TimingPoint"/> 
/// in <see cref="Timing.TimingPoints"/> if the subsequent point exists.
/// </summary>
public partial class TimingPoint : Node, IComparable<TimingPoint>, ICloneable
{
	#region Properties and Fields

	public bool IsInstantiating = true;

    public bool IsBeingUpdated = false;

	public ulong SystemTimeWhenCreatedMsec;

	#region Time Signature
	private int[] timeSignature = [4, 4];

	/// <summary>
	/// Time Signature. Affects how <see cref="MeasuresPerSecond"/> and <see cref="TimeSignature"/> correlate
	/// via the formulas <see cref="BpmToMps(float)"/> and <see cref="MpsToBpm(float)"/>.
	/// Changes are always accepted.
	/// </summary>
	public int[] TimeSignature
	{
		get => timeSignature;
		set
		{
			if (timeSignature == value)
				return;
            int[] oldValue = timeSignature;
			timeSignature = value;

            PropertyChanged?.Invoke(this, new PropertyChangeArgument(PropertyType.TimeSignature, oldValue, value));
		}
	}
	#endregion

	#region Offset
	private float offset;

	/// <summary>
	/// The timestamp in the audio which this <see cref="TimingPoint"/> is attached to. 
	/// </summary>
	public float Offset
	{
		get => offset;
		set
		{
            if (offset == value)
                return;
            if (IsInstantiating)
            {
                offset = value;
                return;
            }

            float oldValue = offset;
			offset = value;

            PropertyChanged?.Invoke(this, new PropertyChangeArgument(PropertyType.Offset, oldValue, value));
		}
	}
	#endregion

	#region MusicPosition
	private float? musicPosition;
	/// <summary>
	/// The <see cref="TimingPoint"/>'s position on the musical timeline. 
	/// The value is defined in terms of measures (integer part) from the musical timeline origin.
	/// Individual beats in a measure are the fractional part of the value.
	/// As an example, if a measure has a 4/4 <see cref="TimeSignature"/>, 
	/// the value 0.75 means "Measure 0, Quarter note 4"
	/// </summary>
	public float? MusicPosition
	{
		get => musicPosition;
		set
		{
			if (musicPosition == value)
				return;

            float? oldValue = musicPosition;
            musicPosition = value;

            PropertyChanged?.Invoke(this, new PropertyChangeArgument(PropertyType.MusicPosition, oldValue, value));
        }
	}
	#endregion

	#region MeasuresPerSecond
	private float measuresPerSecond = 0.5f;
	/// <summary>
	/// Musical measures per second. 
	/// Directly correlated with <see cref="Bpm"/> and <see cref="TimeSignature"/>
	/// via the formulas <see cref="BpmToMps(float)"/> and <see cref="MpsToBpm(float)"/>.
	/// Cannot be changed directly, as it is a calculated property via <see cref="MeasuresPerSecond_Set(Timing)"/>
	/// </summary>
	public float MeasuresPerSecond
	{
		get => measuresPerSecond;
		set
		{
			if (measuresPerSecond == value)
				return;

            float oldValue = measuresPerSecond;
			measuresPerSecond = value;

            PropertyChanged?.Invoke(this, new PropertyChangeArgument(PropertyType.MeasuresPerSecond, oldValue, value));
        }
	}
	#endregion

	#region Bpm
	private float bpm;
	/// <summary>
	/// Beats per minute. Directly correlated with <see cref="MeasuresPerSecond"/> and <see cref="TimeSignature"/>
	/// via the formulas <see cref="BpmToMps(float)"/> and <see cref="MpsToBpm(float)"/>.
	/// Changes are only accepted if <see cref="Timing"/> validates the change.
	/// </summary>
	public float Bpm
	{
		get
		{
			if (bpm == 0)
				bpm = MpsToBpm(MeasuresPerSecond);
			return bpm;
		}
		set
		{
			if (bpm == value)
				return;
			
			float oldValue = bpm;
			bpm = Settings.Instance.RoundBPM ? (float)Math.Round(value*10, MidpointRounding.ToEven) / 10f : value;

            PropertyChanged?.Invoke(this, new PropertyChangeArgument(PropertyType.Bpm, oldValue, value));
		}
	}
	#endregion

	#endregion
	#region Constructors
	public TimingPoint(float time, int[] timeSignature)
	{
		this.offset = time;
		this.timeSignature = timeSignature;
		SystemTimeWhenCreatedMsec = Time.GetTicksMsec();
	}

	public TimingPoint(float musicPosition)
	{
		this.musicPosition = musicPosition;
		SystemTimeWhenCreatedMsec = Time.GetTicksMsec();
	}

	public TimingPoint(float time, float musicPosition, float measuresPerSecond)
	{
		this.offset = time;
		this.musicPosition = musicPosition;
		this.measuresPerSecond = measuresPerSecond;
		SystemTimeWhenCreatedMsec = Time.GetTicksMsec();
	}

	public TimingPoint(float time, float musicPosition, int[] timeSignature)
	{
		this.offset = time;
		this.musicPosition = musicPosition;
		this.timeSignature = timeSignature;
		SystemTimeWhenCreatedMsec = Time.GetTicksMsec();
	}

	public TimingPoint(float time, float musicPosition, int[] timeSignature, float measuresPerSecond)
	{
		this.offset = time;
		this.musicPosition = musicPosition;
		this.timeSignature = timeSignature;
		this.measuresPerSecond = measuresPerSecond;
		bpm = MpsToBpm(measuresPerSecond);
		SystemTimeWhenCreatedMsec = Time.GetTicksMsec();
	}

	/// <summary>
	/// Constructor used only for cloning
	/// </summary>
	private TimingPoint(float time, float? musicPosition, int[] timeSignature, float measuresPerSecond, float bpm, bool isInstantiating)
	{
		this.offset = time;
		this.musicPosition = musicPosition;
		this.timeSignature = timeSignature;
		this.measuresPerSecond = measuresPerSecond;
		this.bpm = bpm;
		this.IsInstantiating = isInstantiating;
		SystemTimeWhenCreatedMsec = Time.GetTicksMsec();
	}
	#endregion
	#region Interface Methods
	public int CompareTo(TimingPoint? other) => Offset.CompareTo(other?.Offset);

	public object Clone()
	{
		var timingPoint = new TimingPoint(Offset, MusicPosition, TimeSignature, MeasuresPerSecond, Bpm, IsInstantiating);

		return timingPoint;
	}
	#endregion
	#region Change and Deletion Events
	public event EventHandler ChangeFinalized = null!;

	public void FinalizeChange()
	{
		if (!IsInstantiating)
		{
			ChangeFinalized?.Invoke(this, EventArgs.Empty);
		}
	}

	public event EventHandler AttemptDelete = null!;
	/// <summary>
	///     Relies on parent <see cref="Timing" /> to delete from project.
	/// </summary>
	public void Delete() => AttemptDelete?.Invoke(this, EventArgs.Empty);

    public event EventHandler PropertyChanged = null!;

    public enum PropertyType
    {
        TimeSignature,
        Offset,
        MusicPosition,
        MeasuresPerSecond,
        Bpm,
    }

    public class PropertyChangeArgument(PropertyType propertyType, object? oldValue, object? newValue) : EventArgs
    {
        private PropertyType propertyType = propertyType;
        private object? oldValue = oldValue;
        private object? newValue = newValue;
        public object? OldValue
        {
            get => oldValue;
        }
        public object? NewValue
        {
            get => newValue;
        }
        public PropertyType PropertyType
        {
            get => propertyType;
        }
    }
    #endregion
    #region Calculators
    public float BpmToMps(float bpm) => bpm / (60 * (TimeSignature[0] * 4f / TimeSignature[1]));

	public float MpsToBpm(float mps) => mps * 60 * (TimeSignature[0] * 4f / TimeSignature[1]);

	public float BeatLengthSec => 1 / (Bpm / 60);
	#endregion
}
