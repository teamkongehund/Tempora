using Godot;
using System;

public partial class LabeledScrollbar : VBoxContainer
{
	[Export]
	protected string title;
	[Export]
	protected double minValue = 0;
	[Export]
	protected double maxValue = 1;
	[Export]
	protected double step = 0.01;

	protected Label titleLabel;
	protected HScrollBar hScrollBar;
	protected Label valueLabel;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		titleLabel = GetNode<Label>("TitleLabel");
		hScrollBar = GetNode<HScrollBar>("HScrollBar");
        valueLabel = hScrollBar.GetNode<Label>("ValueLabel");

        //hScrollBar.Changed += OnHScrollBarChanged;

        hScrollBar.ValueChanged += OnValueChanged;

		//hScrollBar.AllowGreater = true;
  //      hScrollBar.AllowLesser = true;

        SetInitialValue();
		UpdateValueLabel();

		hScrollBar.MinValue = minValue;
		hScrollBar.MaxValue = maxValue;
		hScrollBar.Step = step;
		titleLabel.Text = title;

        //hScrollBar.AllowGreater = false;
        //hScrollBar.AllowLesser = false;
    }

	protected virtual void OnValueChanged(double value)
	{
		UpdateValueLabel();
		UpdateValue();
	}

    protected virtual void UpdateValueLabel()
    {
        valueLabel.Text = hScrollBar.Value.ToString("0.00");
    }

	protected virtual void UpdateValue()
	{
		GD.Print("No value updated. UpdateValue() must be overridden.");
	}

	protected virtual void SetInitialValue()
	{
		hScrollBar.Value = hScrollBar.MinValue;
	}

    //private void OnHScrollBarChanged()
    //{
    //    GD.Print($"Min = {hScrollBar.MinValue}, Max = {hScrollBar.MaxValue}, Step = {hScrollBar.Step}, Value = {hScrollBar.Value}");
    //}
}
