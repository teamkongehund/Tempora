using Godot;
using System;

public partial class Stepper : Control

{
    [Export]
    Button incrementButton = null!;

    [Export]
    protected Label valueLabel = null!;
    
    [Export]
    Button decrementButton = null!;

    [Export]
    Control mouseAreaControl = null!;

    [Export]
    protected int increment = 1;

    [Export]
    public int value = 0;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        UpdateValue(value);
        incrementButton.Pressed += OnIncrementButtonPressed;
        decrementButton.Pressed += OnDecrementButtonPressed;
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        Vector2 localMousePosition = GetLocalMousePosition();
        Rect2 rectangle = mouseAreaControl.GetRect();
        bool hasMouseInside = rectangle.HasPoint(localMousePosition + Position);

        decrementButton.Visible = hasMouseInside;
        incrementButton.Visible = hasMouseInside;
    }

    protected virtual void OnIncrementButtonPressed()
    {
        UpdateValueAndTarget(value + 1);
    }
    protected virtual void OnDecrementButtonPressed()
    {
        UpdateValueAndTarget(value - 1);
    }

    protected void UpdateValueAndTarget(int value)
    {
        UpdateValue(value);
        UpdateTarget();
    }

    protected virtual void UpdateValue(int value)
    {
        this.value = value;
        valueLabel.Text = value.ToString();
    }

    protected virtual void UpdateTarget() => GD.Print("No value updated. UpdateValue() must be overridden.");
}
